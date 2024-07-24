using System.Threading.Tasks;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class SendItemCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        private readonly ITwitchUserStore userStore;

        public SendItemCommandProcessor(IRavenfallClient game, IUserProvider playerProvider, ITwitchUserStore userStore)
        {
            this.RequiresBroadcaster = true;
            this.game = game;
            this.playerProvider = playerProvider;
            this.userStore = userStore;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);
            var query = cmd.Arguments?.Trim();
            await this.game[cmd.CorrelationId].SendItemAsync(player, query);
        }
    }
}
