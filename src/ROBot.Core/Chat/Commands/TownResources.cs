using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class TownResources : ChatCommandHandler
    {
        public override string Description => "Gets the resources available in this town (ore, fish, wheat and coins)";
        public override string UsageExample => "!townresources";
        public override string Category => "Items";
        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var player = session.Get(cmd);
                    await connection[cmd].RequestTownResourcesAsync(player);
                }
            }
        }
    }
}
