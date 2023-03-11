using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class RacingCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public RacingCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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
                await this.game.Reply(cmd.CorrelationId).PlayPetRacingAsync(player);
                return;
            }

            if (cmd.Arguments.Trim().Equals("reset", System.StringComparison.OrdinalIgnoreCase))
            {
                await this.game.Reply(cmd.CorrelationId).ResetPetRacingAsync(player);
                return;
            }
        }
    }
}