using Microsoft.Extensions.Logging;
using Shinobytes.Network;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Threading;

namespace ROBot
{
    public class AdminTcpAPIEndpointServer : IAdminAPIEndpointServer, IDisposable
    {
        private readonly ILogger logger;

        private readonly IServer server;
        private readonly IKernel kernel;
        private readonly IServerPacketHandlerProvider packetHandler;
        private readonly IServerPacketSerializer packetSerializer;

        private int retryWaitSeconds = 3;
        private int retryWaitMaxSeconds = 30;

        public AdminTcpAPIEndpointServer(
            ILogger logger,
            IServer server,
            IKernel kernel,
            IServerPacketHandlerProvider packetHandler,
            IServerPacketSerializer packetSerializer)
        {
            this.logger = logger;
            this.packetHandler = packetHandler;
            this.packetSerializer = packetSerializer;
            this.server = server;
            this.kernel = kernel;

            this.server.ClientConnected += Server_ClientConnected;
            this.server.ClientDisconnected += Server_ClientDisconnected;

            StartServer();
        }
        private async void StartServer()
        {
            if (!await server.StartAsync(CancellationToken.None))
            {
                // reschedule the start.
                logger.LogWarning("Failed to start Admin API Endpoint Server. Retrying in " + retryWaitSeconds + " seconds.");
                kernel.SetTimeout(() => StartServer(), retryWaitSeconds * 1000);
                retryWaitSeconds = System.Math.Min(retryWaitMaxSeconds, retryWaitSeconds + 1);
                return;
            }

            logger.LogInformation("Admin API Endpoint Server Started");
            retryWaitSeconds = 3;
        }

        public void Dispose()
        {
            this.server.ClientConnected -= Server_ClientConnected;
            this.server.ClientDisconnected -= Server_ClientDisconnected;
            this.server.Dispose();
        }

        private void ServerClient_DataReceived(object sender, DataPacket e)
        {
            //WriteLine("[Debug]: Log Server Recieved Data: " + e.Length);

            var client = sender as INetworkClient;
            var packet = packetSerializer.Deserialize(e);
            if (e == null)
            {
                logger.LogError("[Admin API] Bad Packet Data Recieved");
                return;
            }

            var handler = packetHandler.Get(packet.Type);
            if (handler == null)
            {
                logger.LogError("[Admin API] No packet handler available for " + packet.Type);
                return;
            }

            handler.HandleAsync(client, packet);
        }

        private void Server_ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            e.Client.DataReceived -= ServerClient_DataReceived;
        }

        private void Server_ClientConnected(object sender, ConnectionEventArgs e)
        {
            e.Client.DataReceived += ServerClient_DataReceived;
        }
    }
}
