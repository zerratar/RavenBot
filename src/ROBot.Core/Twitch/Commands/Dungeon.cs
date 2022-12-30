using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Dungeon : TwitchCommandHandler
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

                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await connection.JoinDungeonAsync(new EventJoinRequest(player, null));
                        return;
                    }
                    else if (cmd.Arguments.Contains("stop", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (player.IsBroadcaster || player.IsModerator)
                        {
                            await connection.StopDungeonAsync(player);
                        }
                    }
                    else if (cmd.Arguments.Contains("start", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await connection.DungeonStartAsync(player);
                    }
                }
            }
        }
    }
}
