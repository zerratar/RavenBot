using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Kick : TwitchCommandHandler
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
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                    {
                        twitch.Broadcast(channel, cmd.Sender.Username, Localization.KICK_PERM);
                        return;
                    }

                    var targetPlayerName = cmd.Arguments?.Trim();
                    if (string.IsNullOrEmpty(targetPlayerName))
                    {
                        twitch.Broadcast(channel, cmd.Sender.Username, Localization.KICK_NO_USER);
                        return;
                    }

                    await connection.KickAsync(session.GetUserByName(targetPlayerName));
                }
            }
        }
    }
}
