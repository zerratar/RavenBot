using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class DropEventCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public DropEventCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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

            if (!cmd.Sender.IsBroadcaster)
            {
                //broadcaster.Broadcast(

                broadcaster.Send(cmd.Sender.Username,
                    "You do not have permission to kick a player from the game.");
                return;
            }

            var item = cmd.Arguments?.Trim();

            if (string.IsNullOrEmpty(item))
            {
                return;
            }

            var player = playerProvider.Get(cmd.Sender);
            await game.ItemDropEventAsync(player, item);
        }
    }
}
