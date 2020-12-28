using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ExpMultiplierLimitProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ExpMultiplierLimitProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.RequiresBroadcaster = true;
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }

            var numOfSubs = 1;
            if (!string.IsNullOrEmpty(cmd.Arguments))
            {
                int.TryParse(cmd.Arguments, out numOfSubs);
            }
            if (numOfSubs < 15) return;

            var player = playerProvider.Get(cmd.Sender);
            await game.SetExpMultiplierLimitAsync(player, numOfSubs);
        }
    }
}