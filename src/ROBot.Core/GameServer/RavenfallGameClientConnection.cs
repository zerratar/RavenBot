using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RavenBot.Core.Extensions;
using RavenBot.Core.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ROBot.Core.GameServer
{
    public class RavenfallGameClientConnection : IGameClient
    {
        private readonly object mutex = new object();
        // one writer at a time: pings, chat handlers and message bus callbacks all send from different threads.
        private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);
        private readonly List<Subscription> subs = new List<Subscription>();
        private readonly IBotServer server;
        private readonly ILogger logger;
        private TcpClient connection;

        private StreamWriter writer;
        private StreamReader reader;

        private Thread gameReaderThread;
        private bool readActive;
        private int disposed;
        private int disconnected;

        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public RavenfallGameClientConnection(TcpClient client, ILogger logger)
        {
            this.connection = client;
            this.logger = logger;

            if (IsConnected)
            {
                var stream = connection.GetStream();
                this.writer = new StreamWriter(stream);
                this.reader = new StreamReader(stream);
                BeginReceiveData();
                OnConnectionEstablished();
            }
        }

        public IGameSession Session { get; internal set; }

        public bool IsConnected => this.connection != null && this.connection.Connected;

        public IPEndPoint EndPoint
        {
            get
            {
                if (this.connection == null || this.connection.Client == null)
                {
                    return null;
                }

                if (this.connection.Client.RemoteEndPoint != null)
                {
                    return this.connection.Client.RemoteEndPoint as IPEndPoint;
                }

                return null;
            }
        }

        public Task<bool> ProcessAsync(int serverPort)
        {
            return Task.FromResult(IsConnected);
        }

        private void BeginReceiveData()
        {
            if (this.gameReaderThread?.ThreadState == ThreadState.Running)
                this.gameReaderThread.Join();

            this.gameReaderThread = new Thread(ReadFromGameProcess);
            this.gameReaderThread.Start();
        }

        private void OnConnectionEstablished()
        {
            if (!IsConnected) return;
            Connected?.Invoke(this, EventArgs.Empty);
        }

        public IGameClientSubcription Subscribe(string cmdIdentifier, Action<GameMessageResponse> onCommand)
        {
            lock (mutex)
            {
                var sub = new Subscription(this, cmdIdentifier, onCommand);
                this.subs.Add(sub);
                return sub;
            }
        }

        private void Unsubscribe(Subscription subscription)
        {
            lock (mutex)
            {
                this.subs.Remove(subscription);
            }
        }

        private async void ReadFromGameProcess()
        {
            readActive = true;
            while (readActive)
            {
                try
                {
                    var r = this.reader;
                    if (r == null || !r.BaseStream.CanRead)
                    {
                        GameDisconnected();
                        return;
                    }

                    var message = await r.ReadLineAsync();

                    // only the end of the stream means the game went away, an empty line is just skipped.
                    if (message == null)
                    {
                        GameDisconnected();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        continue;
                    }

                    HandleMessage(message);
                }
                catch (IOException)
                {
                    GameDisconnected();
                    return;
                }
                catch (ObjectDisposedException)
                {
                    GameDisconnected();
                    return;
                }
                catch (Exception exc)
                {
                    logger.LogError(exc.ToString());
                    await Task.Delay(1000);
                }
            }
        }

        private void GameDisconnected()
        {
            // the reader, a failed write and the heartbeat can all notice the drop; only report it once.
            if (Interlocked.Exchange(ref this.disconnected, 1) == 1)
            {
                Dispose();
                return;
            }

            //logger.LogDebug("Game Disconnected");
            if (Disconnected != null)
                Disconnected.Invoke(this, EventArgs.Empty);

            Dispose();
        }

        private void HandleMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            try
            {
                var packet = JsonConvert.DeserializeObject<GameMessageResponse>(message);
                if (packet != null)
                {
                    lock (mutex)
                    {
                        foreach (var sub in subs.Where(x => x.Identifier == packet.Identifier))
                        {
                            sub.Invoke(packet);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                logger.LogError("Failed to handle message, content: " + message + ", exception: " + exc.ToString());
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 1) return;
            readActive = false;

            var c = connection;
            if (c != null)
            {
                // close the socket first so a write stuck on a full buffer fails instead of blocking the dispose.
                try { c.Close(); } catch { }
                try { writer?.Dispose(); } catch { }
                try { reader?.Dispose(); } catch { }
                try { c.Dispose(); } catch { }
                writer = null;
                reader = null;
                connection = null;
            }
        }

        public async Task SendAsync(string message)
        {
            if (this.writer == null || this.disposed == 1)
            {
                return;
            }

            var failed = false;
            await this.writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var w = this.writer;
                if (w != null && w.BaseStream.CanWrite)
                {
                    await w.WriteLineAsync(message.AsUTF8()).ConfigureAwait(false);
                    await w.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (Exception exc)
            {
                this.logger.LogError(exc.ToString());
                failed = true;
            }
            finally
            {
                this.writeLock.Release();
            }

            if (failed)
            {
                GameDisconnected();
            }
        }

        internal void Close()
        {
            try
            {
                GameDisconnected();
            }
            catch
            {
            }
        }

        private class Subscription : IGameClientSubcription
        {
            public readonly string Identifier;
            private readonly RavenfallGameClientConnection client;
            private readonly Action<GameMessageResponse> onCommand;

            public Subscription(RavenfallGameClientConnection client, string identifier, Action<GameMessageResponse> onCommand)
            {
                this.client = client;
                this.Identifier = identifier;
                this.onCommand = onCommand;
            }

            public void Invoke(GameMessageResponse command)
            {
                this.onCommand?.Invoke(command);
            }

            public void Unsubscribe()
            {
                this.client.Unsubscribe(this);
            }
        }

    }
}