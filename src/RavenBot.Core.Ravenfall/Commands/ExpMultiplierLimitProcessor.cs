using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ExpMultiplierLimitProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public ExpMultiplierLimitProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.RequiresBroadcaster = true;
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

            var numOfSubs = 1;
            if (!string.IsNullOrEmpty(cmd.Arguments))
            {
                int.TryParse(cmd.Arguments, out numOfSubs);
            }
            if (numOfSubs < 15) return;

            var player = playerProvider.Get(cmd);
            await this.game.Reply(cmd.CorrelationId).SetExpMultiplierLimitAsync(player, numOfSubs);
        }
    }
}