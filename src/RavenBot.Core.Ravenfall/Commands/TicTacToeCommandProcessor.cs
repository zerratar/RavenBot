using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class TicTacToeCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        public TicTacToeCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Send(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
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