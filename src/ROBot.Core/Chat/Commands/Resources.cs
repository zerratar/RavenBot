using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Resources : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Gets how much resources are available on your account (ore, fish, wheat, coins). This will soon be obsolete as player resources are being replaced by items instead.";
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
                    await connection[cmd].RequestPlayerResourcesAsync(player);
                }
            }
        }
    }
}
