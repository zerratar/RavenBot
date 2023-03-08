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

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);

            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                await game.ActivateTicTacToeAsync(player);
                return;
            }

            if (cmd.Arguments.Trim().Equals("reset", System.StringComparison.OrdinalIgnoreCase))
            {
                await game.ResetTicTacToeAsync(player);
                return;
            }

            if (int.TryParse(cmd.Arguments.Trim(), out var num))
            {
                await game.PlayTicTacToeAsync(player, num);
            }
        }
    }
}