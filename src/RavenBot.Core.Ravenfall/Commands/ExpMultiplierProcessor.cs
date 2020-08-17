using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{

    public class ExpMultiplierProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ExpMultiplierProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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

            var numOfSubs = 1;
            if (!string.IsNullOrEmpty(cmd.Arguments))
            {
                int.TryParse(cmd.Arguments, out numOfSubs);
            }

            var player = playerProvider.Get(cmd.Sender);
            await game.SetExpMultiplierAsync(player, numOfSubs);
        }
    }
}