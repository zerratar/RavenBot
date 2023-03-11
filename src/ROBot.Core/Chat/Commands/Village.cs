using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Village : ChatCommandHandler
    {
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
                    if (!string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await connection.Reply(cmd.CorrelationId).SetAllVillageHutsAsync(player, cmd.Arguments);
                    }
                    else
                    {
                        await connection.Reply(cmd.CorrelationId).GetVillageBoostAsync(player);
                    }
                }
            }
        }
    }
}
