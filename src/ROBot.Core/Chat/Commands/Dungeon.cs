using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Dungeon : ChatCommandHandler
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

                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await connection.Reply(cmd.CorrelationId).JoinDungeonAsync(player, null);
                        return;
                    }
                    else if (cmd.Arguments.Contains("stop", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (player.IsBroadcaster || player.IsModerator)
                        {
                            await connection.Reply(cmd.CorrelationId).StopDungeonAsync(player);
                        }
                    }
                    else if (cmd.Arguments.Contains("start", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await connection.Reply(cmd.CorrelationId).DungeonStartAsync(player);
                    }
                }
            }
        }
    }
}
