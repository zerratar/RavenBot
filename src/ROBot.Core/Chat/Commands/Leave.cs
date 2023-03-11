using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Chat.Commands
{
    public class Leave : ChatCommandHandler
    {
        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null)
                return;

            // client might not accept a leave.
            //session.Leave(cmd.Sender.UserId);

            var connection = game.GetConnection(session);
            if (connection != null)
            {
                var player = session.Get(cmd);
                if (player != null)
                {
                    await connection.Reply(cmd.CorrelationId).LeaveAsync(player);
                }
            }
        }
    }
}
