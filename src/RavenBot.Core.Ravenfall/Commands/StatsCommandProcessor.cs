using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ApperanceCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ApperanceCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                //broadcaster.Broadcast(

                broadcaster.Send(cmd.Sender.Username,
                    Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);
            if (string.IsNullOrEmpty(cmd.Arguments))
            {

                broadcaster.Broadcast("You can customize your character here https://www.ravenfall.stream/customization");
                return;
            }

            await this.game.PlayerAppearanceUpdateAsync(player, cmd.Arguments);
        }
    }

    public class StatsCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public StatsCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                //broadcaster.Broadcast(

                broadcaster.Send(cmd.Sender.Username,
                    Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);
            await this.game.RequestPlayerStatsAsync(player, cmd.Arguments);
        }
    }
}