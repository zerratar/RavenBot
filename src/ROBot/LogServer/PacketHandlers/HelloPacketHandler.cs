using Microsoft.Extensions.Logging;
using Shinobytes.Network;
using System;
using System.Threading.Tasks;

namespace ROBot.LogServer.PacketHandlers
{
    public class HelloPacketHandler : IServerPacketHandler
    {
        private readonly ILogger logger;

        public HelloPacketHandler(ILogger logger)
        {
            this.logger = logger;
        }

        public Task HandleAsync(INetworkClient client, ServerPacket packet)
        {
            this.logger.LogDebug("Hello Packet Received");

            return Task.CompletedTask;
        }
    }
}
