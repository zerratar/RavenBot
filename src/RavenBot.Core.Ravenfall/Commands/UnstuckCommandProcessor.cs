using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class UnstuckCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public UnstuckCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }
            var player = playerProvider.Get(cmd.Sender);
            if (!string.IsNullOrEmpty(cmd.Arguments) && (cmd.Sender.IsBroadcaster || cmd.Sender.IsModerator || cmd.Sender.IsGameAdmin || cmd.Sender.IsGameModerator))
            {
                if (cmd.Arguments.Trim().Equals("all", System.StringComparison.OrdinalIgnoreCase) ||
                    cmd.Arguments.Trim().Equals("everyone", System.StringComparison.OrdinalIgnoreCase) ||
                    cmd.Arguments.Trim().Equals("training", System.StringComparison.OrdinalIgnoreCase))
                {
                    await this.game[cmd.CorrelationId].UnstuckAsync(player, cmd.Arguments);
                    return;
                }

                player = playerProvider.Get(cmd.Arguments);
                if (player != null)
                    await this.game[cmd.CorrelationId].UnstuckAsync(player, cmd.Arguments);
            }
            else
            {
                player = playerProvider.Get(cmd.Sender, cmd.Arguments);
                await this.game[cmd.CorrelationId].UnstuckAsync(player, cmd.Arguments);
            }

        }
    }
}