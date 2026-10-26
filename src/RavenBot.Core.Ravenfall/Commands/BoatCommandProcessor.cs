using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    /// <summary>!boat: the player's own boat; !boat build fishing boat (or sloop) at the shipwright on Away; !boat list.</summary>
    public class BoatCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public BoatCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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

            var player = playerProvider.Get(cmd);
            await this.game[cmd.CorrelationId].BoatAsync(player, cmd.Arguments ?? string.Empty);
        }
    }
}
