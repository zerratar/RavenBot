using Microsoft.Extensions.Logging;
using Shinobytes.Network;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ROBot
{
    public class ConsoleLogServer : ILogger, IDisposable
    {

        const string logsDir = "../logs";
        const double logsLifespanDays = 7;

        private readonly ConsoleLogger logger;
        private readonly IServer server;
        private readonly IMessageBus messageBus;
        private readonly IServerConnectionManager connectionManager;
        private readonly IServerPacketHandlerProvider packetHandler;
        private readonly IServerPacketSerializer packetSerializer;

        private readonly object mutex = new object();
        private readonly List<string> messages = new List<string>();
        private readonly int maxMessageStack = 1000;

        public ConsoleLogServer(
            IServer server,
            IMessageBus messageBus,
            IServerConnectionManager connectionManager,
            IServerPacketHandlerProvider packetHandler,
            IServerPacketSerializer packetSerializer)
        {
            this.logger = new ConsoleLogger();
            this.packetHandler = packetHandler;
            this.packetSerializer = packetSerializer;
            this.server = server;
            this.messageBus = messageBus;
            this.connectionManager = connectionManager;
            SetupServer();
        }
        private async void SetupServer()
        {
            this.messageBus.Subscribe("exit", () =>
            {
                TrySaveLogToDisk();
            });

            this.messageBus.Subscribe<INetworkClient>("hello", OnHello);
            this.server.ClientConnected += Server_ClientConnected;
            this.server.ClientDisconnected += Server_ClientDisconnected;
            await server.StartAsync(CancellationToken.None);
            logger.LogInformation("[LOG] Log Server Started");
            Broadcast("{Information}: [LOG] Log Server Started");
        }

        public void TrySaveLogToDisk()
        {
            lock (mutex)
            {
                try
                {
                    var fn = DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log";

                    if (!System.IO.Directory.Exists(logsDir))
                    {
                        System.IO.Directory.CreateDirectory(logsDir);
                    }

                    System.IO.File.AppendAllLines(System.IO.Path.Combine(logsDir, fn), messages);
                }
                catch (System.Exception exc)
                {
                    System.Console.WriteLine(exc.ToString());
                }
                finally
                {
                    CleanupLogs();
                }
            }
        }

        private void CleanupLogs()
        {
            if (!System.IO.Directory.Exists(logsDir))
            {
                return;
            }

            var logs = System.IO.Directory.GetFiles(logsDir, "*.log");
            foreach (var log in logs)
            {
                try
                {
                    var fi = new FileInfo(log);
                    if (fi.CreationTimeUtc >= DateTime.UtcNow.AddDays(logsLifespanDays))
                    {
                        fi.Delete();
                    }
                }
                catch { }
            }
        }

        private void OnHello(INetworkClient client)
        {
            lock (mutex)
            {
                foreach (var msg in messages)
                {
                    var data = UTF8Encoding.UTF8.GetBytes(msg);
                    client.Send(data, 0, data.Length);
                }
            }
        }

        public void Dispose()
        {
            this.server.ClientConnected -= Server_ClientConnected;
            this.server.ClientDisconnected -= Server_ClientDisconnected;
            this.server.Dispose();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.logger.Log<TState>(logLevel, eventId, state, exception, formatter);

            var message = formatter != null ? formatter(state, exception) : state.ToString();
            Broadcast("{" + logLevel + "}: " + message);
        }

        public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state) => logger.BeginScope<TState>(state);

        public void Write(string message)
        {
            this.logger.Write(message);
            Broadcast(message);
        }

        public void WriteLine(string message)
        {
            this.logger.WriteLine(message);
            Broadcast(message);
        }

        private void Broadcast(string v)
        {
            var str = $"[{DateTime.UtcNow:yyyy-MM-dd hh:mm:ss K}]: {v}" + Environment.NewLine;
            var connections = connectionManager.All();

            AddMessage(str);

            if (connections.Count == 0)
            {
                return;
            }

            var data = UTF8Encoding.UTF8.GetBytes(str);
            foreach (var connection in connections)
            {
                if (connection.IsReady)
                {
                    connection.Send(data, 0, data.Length);
                }
            }
        }

        private void AddMessage(string str)
        {
            lock (mutex)
            {
                messages.Add(str);

                if (messages.Count > maxMessageStack)
                {
                    TrySaveLogToDisk();
                    messages.Clear();
                }
            }
        }

        private void ServerClient_DataReceived(object sender, DataPacket e)
        {
            //WriteLine("[Debug]: Log Server Recieved Data: " + e.Length);

            var client = sender as INetworkClient;
            var packet = packetSerializer.Deserialize(e);
            if (e == null)
            {
                WriteLine("{Debug}: [LOG] Bad Packet Data recieved");
                return;
            }

            var handler = packetHandler.Get(packet.Type);
            if (handler == null)
            {
                WriteLine("{Debug}: [LOG] No packet handler available for " + packet.Type);
                return;
            }

            handler.HandleAsync(client, packet);
        }

        private void Server_ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            //WriteLine("[Debug]: Log Server Client disconnected");
            e.Client.DataReceived -= ServerClient_DataReceived;
        }

        private void Server_ClientConnected(object sender, ConnectionEventArgs e)
        {
            //WriteLine("[Debug]: Log Server Client connected");
            e.Client.DataReceived += ServerClient_DataReceived;

        }
    }
}
