using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Villagers : ChatCommandHandler
    {

        public override string Description => "This command allows getting details about the assigned players in your village.";
        public override string UsageExample => "!villagers";
        public override string Category => "Game";

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
                    await connection[cmd].GetVillagersInfoAsync(player);
                }
            }
        }
    }
}
