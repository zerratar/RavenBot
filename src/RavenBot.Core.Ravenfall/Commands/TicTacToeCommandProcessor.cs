using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class TicTacToeCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public TicTacToeCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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

            var player = playerProvider.Get(cmd);

            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                await this.game.Reply(cmd.CorrelationId).ActivateTicTacToeAsync(player);
                return;
            }

            if (cmd.Arguments.Trim().Equals("reset", System.StringComparison.OrdinalIgnoreCase))
            {
                await this.game.Reply(cmd.CorrelationId).ResetTicTacToeAsync(player);
                return;
            }

            if (int.TryParse(cmd.Arguments.Trim(), out var num))
            {
                await this.game.Reply(cmd.CorrelationId).PlayTicTacToeAsync(player, num);
            }
        }
    }
}