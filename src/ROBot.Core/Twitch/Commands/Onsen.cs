using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Onsen : TwitchCommandHandler
    {
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

                    var leaveOnsen = !string.IsNullOrEmpty(cmd.Arguments) && cmd.Arguments.Contains("leave", StringComparison.OrdinalIgnoreCase);
                    if (leaveOnsen)
                    {
                        await connection.LeaveOnsenAsync(player);
                    }
                    else
                    {
                        await connection.JoinOnsenAsync(player);
                    }
                }
            }
        }
    }
}
