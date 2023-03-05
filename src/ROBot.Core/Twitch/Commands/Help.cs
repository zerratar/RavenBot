using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Twitch.Commands
{
    public class Help : TwitchCommandHandler
    {
        public override Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            var channel = cmd.Channel;
            return twitch.SendChatMessageAsync(channel, "No help available at this time.");
        }
    }
}
