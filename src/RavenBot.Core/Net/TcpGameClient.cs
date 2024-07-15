using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenBot.Core.Extensions;
using Shinobytes.Core;

namespace RavenBot.Core.Net
{
    public class TcpGameClient : IGameClient
    {
        private readonly IGameConnection connection;
        private readonly ILogger logger;
        private readonly object mutex = new object();
        private readonly List<Subscription> subs = new List<Subscription>();

        private Thread gameReaderThread;
        private bool isConnected;
        private bool readActive;
        private bool disposed;

        public event EventHandler Connected;

        public TcpGameClient(IGameConnection connection, ILogger logger)
        {
            this.connection = connection;
            this.logger = logger;
        }


        public async Task<bool> ProcessAsync(int serverPort)
        {
            try
            {
                if (!ConnectionAvailable)
                {
                    if (await this.connection.ConnectAsync(serverPort))
                    {
                        OnConnectionEstablished();
                    }
                }

                if (!ConnectionAvailable)
                {
                    return false;
                }

                if (!readActive)
                {
                    BeginReceiveData();
                }

                return true;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }

            return false;
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
            if (!this.connection.IsConnected) return;
            logger.WriteDebug("Connected to Ravenfall");
            Connected?.Invoke(this, EventArgs.Empty);
            isConnected = true;
        }

        private bool ConnectionAvailable => this.connection.IsConnected && isConnected;

        public IGameClientSubcription Subscribe(string cmdIdentifier, Action<GameMessageResponse> onCommand)
        {
            lock (mutex)
            {
                var sub = new Subscription(this, cmdIdentifier, onCommand);
                this.subs.Add(sub);
                return sub;
            }
        }

        public bool IsConnected => this.connection.IsConnected;

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
                    var message = await this.connection.ReceiveAsync();

                    if (message == null)
                    {
                        DisconnectedFromServer();
                        continue;
                    }

                    if (!IsValidMessage(message))
                    {
                        logger.WriteWarning("Received jibberish from the game: " + message);
                        continue;
                    }

                    //Console.WriteLine($"We received a message from the game: {message}");
                    HandleMessage(message);
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.ToString());
                    await Task.Delay(1000);
                }
            }
        }

        private bool IsValidMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            if (!message.Contains("{") || !message.Contains("}"))
            {
                return false;
            }

            return true;
        }

        private void DisconnectedFromServer()
        {
            logger.WriteDebug("Disconnected from Ravenfall");
            isConnected = false;
            readActive = false;
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
                logger.WriteError("Failed to handle message: " + exc.Message + "\nData from Client: " + message);
            }
        }

        public void Dispose()
        {
            if (this.disposed) return;
            readActive = false;
            if (connection.IsConnected)
                connection.Disconnect();
            this.disposed = true;
        }

        public Task SendAsync(string message)
        {
            return this.connection.SendAsync(message.AsUTF8());
        }
        private class Subscription : IGameClientSubcription
        {
            public readonly string Identifier;
            private readonly TcpGameClient client;
            private readonly Action<GameMessageResponse> onCommand;

            public Subscription(TcpGameClient client, string identifier, Action<GameMessageResponse> onCommand)
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