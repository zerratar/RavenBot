using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Help : ChatCommandHandler
    {
        public override Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            return chat.SendChatMessageAsync(channel, "No help available at this time.");
        }
    }
}
