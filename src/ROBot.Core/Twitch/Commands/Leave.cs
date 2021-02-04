using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Twitch.Commands
{
    public class Leave : TwitchCommandHandler
    {
        public override async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null)
                return;

            // client might not accept a leave.
            //session.Leave(cmd.Sender.UserId);

            var connection = game.GetConnection(session);
            if (connection != null)
            {
                var player = session.Get(cmd.Sender);
                if (player != null)
                {
                    await connection.LeaveAsync(player);
                }
            }
        }
    }
}
