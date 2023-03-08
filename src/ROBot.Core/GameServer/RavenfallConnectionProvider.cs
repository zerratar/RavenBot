using RavenBot.Core;
using RavenBot.Core.Ravenfall.Commands;
using System.Net.Sockets;

namespace ROBot.Core.GameServer
{
    public class RavenfallConnectionProvider : IRavenfallConnectionProvider
    {
        private readonly Shinobytes.Ravenfall.RavenNet.Core.IMessageBus messageBus;
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly Shinobytes.Ravenfall.RavenNet.Core.IKernel kernel;
        private readonly IUserProvider playerProvider;

        public RavenfallConnectionProvider(
            Shinobytes.Ravenfall.RavenNet.Core.IMessageBus messageBus,
            Microsoft.Extensions.Logging.ILogger logger,
            Shinobytes.Ravenfall.RavenNet.Core.IKernel kernel,
            IUserProvider playerProvider)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.playerProvider = playerProvider;
            this.messageBus = messageBus;
        }

        public IRavenfallConnection Get(IBotServer server, TcpClient client)
        {
            return new RavenfallConnection(
                logger,
                kernel,
                server,
                playerProvider,
                messageBus,
                new RavenfallGameClientConnection(client, logger));
        }
    }
}