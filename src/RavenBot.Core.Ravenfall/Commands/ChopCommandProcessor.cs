using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ChopCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public ChopCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                chat.SendReply(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var query = cmd.Arguments?.Trim();
            var player = playerProvider.Get(cmd);
            await this.game[cmd.CorrelationId].ChopAsync(player, query);
        }
    }
}