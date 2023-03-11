using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class KickCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public KickCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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

            if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
            {
                chat.SendReply(cmd, Localization.KICK_PERM);
                return;
            }

            var targetPlayerName = cmd.Arguments?.Trim();
            if (string.IsNullOrEmpty(targetPlayerName))
            {
                chat.SendReply(cmd, Localization.KICK_NO_USER);
                return;
            }
            var sender = playerProvider.Get(cmd);
            var targetPlayer = playerProvider.Get(targetPlayerName);
            await this.game.Reply(cmd.CorrelationId).KickAsync(sender, targetPlayer);
        }
    }
}
