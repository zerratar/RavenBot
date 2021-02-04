using RavenBot.Core.Ravenfall.Commands;
using System.Net.Sockets;

namespace ROBot.Core.GameServer
{
    public class RavenfallConnectionProvider : IRavenfallConnectionProvider
    {
        private readonly Shinobytes.Ravenfall.RavenNet.Core.IMessageBus messageBus;
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly IPlayerProvider playerProvider;

        public RavenfallConnectionProvider(
            Shinobytes.Ravenfall.RavenNet.Core.IMessageBus messageBus,
            Microsoft.Extensions.Logging.ILogger logger,
            IPlayerProvider playerProvider)
        {
            this.logger = logger;
            this.playerProvider = playerProvider;
            this.messageBus = messageBus;
        }

        public IRavenfallConnection Get(IBotServer server, TcpClient client)
        {
            return new RavenfallConnection(
                logger,
                server,
                playerProvider,
                messageBus,
                new RavenfallGameClientConnection(client, logger));
        }
    }
}