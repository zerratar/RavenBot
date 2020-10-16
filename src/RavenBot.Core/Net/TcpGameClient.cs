using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RavenBot.Core.Net
{
    public class TcpGameClient : IGameClient, IGameClient2, IGameClient3
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
            logger.WriteDebug("TcpGameClient::Connected");
            Connected?.Invoke(this, EventArgs.Empty);
            isConnected = true;
        }

        private bool ConnectionAvailable => this.connection.IsConnected && isConnected;

        public IGameClientSubcription Subscribe(string cmdIdentifier, Action<IGameCommand> onCommand)
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
                    if (string.IsNullOrEmpty(message))
                    {
                        DisconnectedFromServer();
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

        private void DisconnectedFromServer()
        {
            logger.WriteDebug("TcpGameClient::Disconnected");
            isConnected = false;
            readActive = false;
        }

        private void HandleMessage(string message)
        {
            //logger.WriteDebug("TcpGameClient::HandleMessage");
            if (string.IsNullOrEmpty(message)) return;

            var packet = JsonConvert.DeserializeObject<GamePacket>(message);
            if (packet != null)
            {
                HandleCommand(packet.Destination, packet.Command, packet.Args);
            }

            //if (message.StartsWith("{"))
            //{
            //    var data = JObject.Parse(message);
            //    var command = data["Type"].Value<string>();
            //    HandleCommand(string.Empty, command, new string[]
            //    {
            //        data["Data"].ToString()
            //    });
            //}
            //else
            //{
            //    // receiver:cmd|arg1|arg2|arg3|arg4
            //    var messageData = message.Split(':');
            //    var fullCommand = messageData[1].Split('|');
            //    var destination = messageData[0];
            //    var correlationId = "";
            //    if (destination.Contains("|"))
            //    {
            //        // we have a correlationId
            //        var destData = destination.Split('|');
            //        correlationId = destData[0];
            //        destination = destData[1];
            //    }
            //    var command = fullCommand[0];
            //    if (fullCommand.Length > 1)
            //    {
            //        var args = fullCommand.Where((x, i) => i != 0).ToArray();
            //        HandleCommand(destination, command, args);
            //    }
            //    else
            //    {
            //        HandleCommand(destination, command);
            //    }
            //}
        }

        private void HandleCommand(string destination, string command, params string[] args)
        {
            lock (mutex)
            {
                foreach (var sub in subs.Where(x => x.Identifier == command))
                {
                    sub.Invoke(new GameCommand(destination, command, args));
                }
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
            return this.connection.SendAsync(message);
        }

        private class Subscription : IGameClientSubcription
        {
            public readonly string Identifier;
            private readonly TcpGameClient client;
            private readonly Action<IGameCommand> onCommand;

            public Subscription(TcpGameClient client, string identifier, Action<IGameCommand> onCommand)
            {
                this.client = client;
                this.Identifier = identifier;
                this.onCommand = onCommand;
            }

            public void Invoke(IGameCommand command)
            {
                this.onCommand?.Invoke(command);
            }

            public void Unsubscribe()
            {
                this.client.Unsubscribe(this);
            }
        }
    }
    public class GamePacket
    {
        public GamePacket(string destination, string command, string[] args)
        {
            this.Destination = destination;
            this.Command = command;
            this.Args = args;
        }

        public string Destination { get; }

        public string Command { get; }

        public string[] Args { get; }
    }
}