using Microsoft.Extensions.Logging;
using Shinobytes.Network;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Threading.Tasks;

namespace ROBot.LogServer.PacketHandlers
{
    public class HelloPacketHandler : IServerPacketHandler
    {
        private readonly ILogger logger;
        private readonly IMessageBus messageBus;

        public HelloPacketHandler(ILogger logger, IMessageBus messageBus)
        {
            this.logger = logger;
            this.messageBus = messageBus;
        }

        public Task HandleAsync(INetworkClient client, ServerPacket packet)
        {
            client.IsReady = true;
            this.logger.LogDebug("[LOG] Hello Packet Received");
            this.messageBus.Send("hello", client);

            return Task.CompletedTask;
        }
    }
}
