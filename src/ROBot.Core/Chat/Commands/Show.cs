using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Show : ChatCommandHandler
    {
        public Show()
        {
            RequiresBroadcaster = true;
        }

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsSubscriber)
                    {
                        chat.SendReply(cmd, Localization.OBSERVE_PERM);
                        return;
                    }

                    var targetPlayerName = cmd.Arguments?.Trim();
                    var player = string.IsNullOrEmpty(targetPlayerName)
                        ? session.Get(cmd.Sender)
                        : session.GetUserByName(targetPlayerName);

                    await connection[cmd.CorrelationId].ObservePlayerAsync(player);
                }
            }
        }
    }
}
