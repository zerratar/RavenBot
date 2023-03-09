using Microsoft.Extensions.Logging;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using Shinobytes.Core;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Discord
{
    public partial class DiscordCommandClient : IDiscordCommandClient
    {
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IBotServer game;
        private readonly IMessageBus messageBus;
        private readonly IUserSettingsManager settingsManager;
        private readonly RavenBot.Core.IChatMessageFormatter messageFormatter;
        private readonly RavenBot.Core.IChatMessageTransformer messageTransformer;
        private readonly IDiscordCommandController commandHandler;

        private IMessageBusSubscription broadcastSubscription;

        public DiscordCommandClient(ILogger logger,
            IKernel kernel,
            IBotServer game,
            IMessageBus messageBus,
            IUserSettingsManager settingsManager,
            RavenBot.Core.IChatMessageFormatter messageFormatter,
            RavenBot.Core.IChatMessageTransformer messageTransformer,
            IDiscordCommandController commandHandler)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.game = game;
            this.messageBus = messageBus;
            this.settingsManager = settingsManager;
            this.messageFormatter = messageFormatter;
            this.messageTransformer = messageTransformer;
            this.commandHandler = commandHandler;

        }

        public void Broadcast(IGameSessionCommand message)
        {
        }

        public void Broadcast(string channel, string user, string format, params object[] args)
        {
        }

        public void SendChatMessage(string channel, string message)
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }

        public Task SendChatMessageAsync(string channel, string message)
        {
            return Task.CompletedTask;
        }
    }
}
