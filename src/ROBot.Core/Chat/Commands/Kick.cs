using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Kick : ChatCommandHandler
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
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                    {
                        chat.SendReply(cmd, Localization.KICK_PERM);
                        return;
                    }

                    var targetPlayerName = cmd.Arguments?.Trim();
                    if (string.IsNullOrEmpty(targetPlayerName))
                    {
                        chat.SendReply(cmd, Localization.KICK_NO_USER);
                        return;
                    }

                    var sender = session.Get(cmd);
                    var target = session.GetUserByName(targetPlayerName);
                    await connection[cmd.CorrelationId].KickAsync(sender, target);
                }
            }
        }
    }
}
