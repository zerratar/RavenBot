using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class RavenfallInfoCommandProcessor : Net.RavenfallCommandProcessor
    {
        public override Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {

            if (cmd.Arguments == null || cmd.Arguments.Length == 0)
            {
                chat.AnnounceAsync("Ravenfall is a Twitch idle game where you can train, craft, fight together against huge raid bosses or fight against eachother.");
            }
            else if (cmd.Arguments.Contains("help", System.StringComparison.OrdinalIgnoreCase))
            {
                chat.AnnounceAsync("Please see https://www.ravenfall.stream/how-to-play on how to play Ravenfall. This guide is still being updated so make sure to check it out frequently.");
            }

            return Task.CompletedTask;
        }
    }
}