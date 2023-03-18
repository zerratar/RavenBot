using RavenBot.Core;
using RavenBot.Core.Ravenfall;
using Shinobytes.Core;
using System.Net.Sockets;

namespace ROBot.Core.GameServer
{
    public class RavenfallConnectionProvider : IRavenfallConnectionProvider
    {
        private readonly IMessageBus messageBus;
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly IKernel kernel;
        private readonly IUserSettingsManager userSettingsManager;
        private readonly IUserProvider playerProvider;

        public RavenfallConnectionProvider(
            IMessageBus messageBus,
            Microsoft.Extensions.Logging.ILogger logger,
            IKernel kernel,
            IUserSettingsManager userSettingsManager,
            IUserProvider playerProvider)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.userSettingsManager = userSettingsManager;
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
                userSettingsManager,
                new RavenfallGameClientConnection(client, logger));
        }
    }
}