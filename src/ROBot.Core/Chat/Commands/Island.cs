using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Island : ChatCommandHandler
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
                    User player = null;
                    if (!string.IsNullOrEmpty(cmd.Arguments))
                    {
                        var username = cmd.Arguments;
                        if (cmd.Arguments.StartsWith("@"))
                        {
                            username = username.Substring(1);
                        }
                        player = session.GetUserByName(username);
                    }

                    if (player == null)
                    {
                        player = session.Get(cmd.Sender);
                    }

                    if (player != null)
                    {
                        await connection.RequestIslandInfoAsync(player);
                    }
                }
            }
        }
    }
}
