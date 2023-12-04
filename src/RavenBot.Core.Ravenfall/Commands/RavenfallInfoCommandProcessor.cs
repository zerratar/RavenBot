using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class RavenfallInfoCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public RavenfallInfoCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (cmd.Arguments == null || cmd.Arguments.Length == 0)
            {
                await chat.AnnounceAsync("Ravenfall is a Twitch idle game where you can train, craft, fight together against huge raid bosses or fight against eachother.");
            }
            else if (cmd.Arguments.StartsWith("reload", System.StringComparison.OrdinalIgnoreCase))
            {
                var player = playerProvider.Get(cmd);
                await this.game[cmd.CorrelationId].ReloadGameAsync(player);
            }
            else if (cmd.Arguments.StartsWith("update", System.StringComparison.OrdinalIgnoreCase))
            {
                var player = playerProvider.Get(cmd);
                await this.game[cmd.CorrelationId].UpdateGameAsync(player);
            }
            else if (cmd.Arguments.Contains("help", System.StringComparison.OrdinalIgnoreCase))
            {
                await chat.AnnounceAsync("Please see https://www.ravenfall.stream/how-to-play on how to play Ravenfall. This guide is still being updated so make sure to check it out frequently.");
            }
        }
    }
}