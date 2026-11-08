using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    /// <summary>Battle pets: !pets (with its action first) and !tame; the game answers. !pet stays the cosmetic pet.</summary>
    public class PetsCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public PetsCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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
            await this.game[cmd.CorrelationId].PetsAsync(player, (cmd.Arguments ?? string.Empty).Trim());
        }
    }

    public class TameCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public TameCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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
            await this.game[cmd.CorrelationId].TameAsync(player, (cmd.Arguments ?? string.Empty).Trim());
        }
    }
}
