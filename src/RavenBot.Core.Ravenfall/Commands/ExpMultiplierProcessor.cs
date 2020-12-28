using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class UseExpMultiplierScrollProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public UseExpMultiplierScrollProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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
                int.TryParse(cmd.Arguments, out numOfSubs);

            var player = playerProvider.Get(cmd.Sender);
            await game.UseExpMultiplierScrollAsync(player, numOfSubs);
        }
    }

    public class ExpMultiplierProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ExpMultiplierProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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
                int.TryParse(cmd.Arguments, out numOfSubs);

            var player = playerProvider.Get(cmd.Sender);
            await game.SetExpMultiplierAsync(player, numOfSubs);
        }
    }
}