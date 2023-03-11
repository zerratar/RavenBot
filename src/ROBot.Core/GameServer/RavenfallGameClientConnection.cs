﻿using Microsoft.Extensions.Logging;
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
        private readonly List<Subscription> subs = new List<Subscription>();
        private readonly IBotServer server;
        private readonly ILogger logger;
        private TcpClient connection;

        private StreamWriter writer;
        private StreamReader reader;

        private Thread gameReaderThread;
        private bool readActive;
        private bool disposed;

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
                    if (!this.reader.BaseStream.CanRead)
                    {
                        GameDisconnected();
                        return;
                    }

                    var message = await this.reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(message))
                    {
                        GameDisconnected();
                        return;
                    }

                    HandleMessage(message);
                }
                catch (IOException exc)
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
            //logger.LogDebug("Game Disconnected");
            if (Disconnected != null)
                Disconnected.Invoke(this, EventArgs.Empty);

            Dispose();
        }

        private void HandleMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

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

        public void Dispose()
        {
            if (this.disposed) return;
            readActive = false;

            if (connection != null)
            {
                try { writer.Dispose(); } catch { }
                try { reader.Dispose(); } catch { }
                try { connection.Close(); } catch { }
                try { connection.Dispose(); } catch { }
                writer = null;
                reader = null;
                connection = null;
            }

            this.disposed = true;
        }

        public async Task SendAsync(string message)
        {
            try
            {
                if (this.writer.BaseStream.CanWrite)
                {
                    await this.writer.WriteLineAsync(message.AsUTF8());
                    await this.writer.FlushAsync();
                    return;
                }
            }
            catch (Exception exc)
            {
                this.logger.LogError(exc.ToString());
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