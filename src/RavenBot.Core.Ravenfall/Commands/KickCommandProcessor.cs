using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class KickCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public KickCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (!await game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }

            if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
            {
                broadcaster.Broadcast(cmd.Sender.Username, "You do not have permission to kick a player from the game.");
                return;
            }

            var targetPlayerName = cmd.Arguments?.Trim();
            if (string.IsNullOrEmpty(targetPlayerName))
            {
                broadcaster.Broadcast(cmd.Sender.Username, "You are kicking who? Provide a username");
                return;
            }

            var targetPlayer = playerProvider.Get(targetPlayerName);
            await game.KickAsync(targetPlayer);
        }
    }
}
