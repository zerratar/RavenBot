using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Night : TwitchCommandHandler
    {
        public Night()
        {
            this.RequiresBroadcaster = true;
        }

        public override async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var player = session.Get(cmd.Sender);
                    if (player != null)
                        await connection.SetTimeOfDayAsync(player, 230, 30);
                }
            }
        }
    }
}
