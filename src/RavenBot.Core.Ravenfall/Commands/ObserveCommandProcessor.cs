using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ObserveCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ObserveCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }
        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {

                broadcaster.Send(cmd.Sender.Username,
                //broadcaster.Broadcast(
                    Localization.GAME_NOT_STARTED);
                return;
            }

            if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
            {
                //broadcaster.Broadcast(
                broadcaster.Send(cmd.Sender.Username,
                    "You do not have permission to set the currently observed player.");
                return;
            }

            var targetPlayerName = cmd.Arguments?.Trim();
            Models.Player player = null;
            if (!string.IsNullOrEmpty(targetPlayerName))
            {
                player = playerProvider.Get(targetPlayerName);
            }
            else
            {
                player = playerProvider.Get(cmd.Sender);
            }

            await game.ObservePlayerAsync(player);
        }
    }
}
