using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    /// <summary>
    /// The banana island's commands (the game's docs/banana-island.md), all sent to the game as one packet with the
    /// command first: !dig joins a treasure dig, !banana (feed, golden) the monkeys' pile, !regatta (start, leave) the
    /// boat race, !pirates calls the pirates' raid. The game answers them all.
    /// </summary>
    public abstract class MangroveCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        private readonly string command;

        protected MangroveCommandProcessor(IRavenfallClient game, IUserProvider playerProvider, string command)
        {
            this.game = game;
            this.playerProvider = playerProvider;
            this.command = command;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);
            await this.game[cmd.CorrelationId].MangroveAsync(player, (command + " " + (cmd.Arguments ?? string.Empty)).Trim());
        }
    }

    public class DigCommandProcessor : MangroveCommandProcessor
    {
        public DigCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider, "dig") { }
    }

    public class BananaCommandProcessor : MangroveCommandProcessor
    {
        public BananaCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider, "banana") { }
    }

    public class RegattaCommandProcessor : MangroveCommandProcessor
    {
        public RegattaCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider, "regatta") { }
    }

    public class PiratesCommandProcessor : MangroveCommandProcessor
    {
        public PiratesCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider, "pirates") { }
    }
}
