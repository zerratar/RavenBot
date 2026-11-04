using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Help : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "Get a link to the full list of game commands on the website.";
        public override string UsageExample => "!help";

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            await chat.SendReplyAsync(cmd, "Please see all available game commands at https://www.ravenfall.stream/commands");
        }
    }
}
