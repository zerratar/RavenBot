using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Gift : ChatCommandHandler
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
                    if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
                    {
                        chat.Broadcast(channel, cmd.Sender.Username, Localization.GIFT_HELP, cmd.Command);
                        return;
                    }

                    var player = session.Get(cmd.Sender);
                    if (player != null)
                        await connection.GiftItemAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
