using Microsoft.Extensions.Logging;
using RavenBot.Core.Net.WebSocket;
using Shinobytes.Network;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ROBot
{
    public class ConsoleLogServer : ILogger, IDisposable
    {
        private readonly ConsoleLogger logger;
        private readonly IServer server;
        private readonly IServerConnectionManager connectionManager;
        private readonly IServerPacketHandlerProvider packetHandler;
        private readonly IServerPacketSerializer packetSerializer;

        private readonly object mutex = new object();
        private readonly List<string> messages = new List<string>();
        private readonly int maxMessageStack = 1000;


        public ConsoleLogServer(
            IServer server,
            IServerConnectionManager connectionManager,
            IServerPacketHandlerProvider packetHandler,
            IServerPacketSerializer packetSerializer)
        {
            this.logger = new ConsoleLogger();
            this.packetHandler = packetHandler;
            this.packetSerializer = packetSerializer;
            this.server = server;
            this.connectionManager = connectionManager;
            SetupServer();
        }
        private async void SetupServer()
        {
            this.server.ClientConnected += Server_ClientConnected;
            this.server.ClientDisconnected += Server_ClientDisconnected;
            await server.StartAsync(CancellationToken.None);
            logger.LogDebug("[Debug]: Log Server Started");
            Broadcast("[Debug]: Log Server Started");
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
            Broadcast("[" + logLevel + "]: " + message);
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
            var str = $"[{DateTime.UtcNow:yyyy-MM-dd hh:mm:ss}]: {v}" + Environment.NewLine;
            var connections = connectionManager.All();

            AddMessage(str);

            if (connections.Count == 0)
            {
                return;
            }

            var data = UTF8Encoding.UTF8.GetBytes(str);
            foreach (var connection in connections)
            {
                connection.Send(data, 0, data.Length);
            }
        }

        private void AddMessage(string str)
        {
            lock (mutex)
            {
                messages.Add(str);

                // relief some pressure on the queue so we dont grow out of memory at some point.
                while (messages.Count > maxMessageStack)
                {
                    messages.RemoveAt(0);
                }
            }
        }

        private void ServerClient_DataReceived(object sender, DataPacket e)
        {
            WriteLine("[Debug]: Log Server Received Data: " + e.Length);

            var client = sender as INetworkClient;
            var packet = packetSerializer.Deserialize(e);
            if (e == null)
            {
                WriteLine("[Debug]: Bad Packet Data Received");
                return;
            }

            var handler = packetHandler.Get(packet.Type);
            if (handler == null)
            {
                WriteLine("[Debug]: No packet handler available for " + packet.Type);
                return;
            }

            handler.HandleAsync(client, packet);
        }

        private void Server_ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            WriteLine("[Debug]: Log Server Client disconnected");
            e.Client.DataReceived -= ServerClient_DataReceived;
        }

        private void Server_ClientConnected(object sender, ConnectionEventArgs e)
        {
            WriteLine("[Debug]: Log Server Client connected");

            e.Client.DataReceived += ServerClient_DataReceived;

            lock (mutex)
            {
                foreach (var msg in messages)
                {
                    var data = UTF8Encoding.UTF8.GetBytes(msg);
                    e.Client.Send(data, 0, data.Length);
                }
            }
        }
    }
}
