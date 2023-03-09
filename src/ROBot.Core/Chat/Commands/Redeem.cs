using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Redeem : ChatCommandHandler
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
                    var player = session.Get(cmd.Sender);
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        chat.Broadcast(channel, cmd.Sender.Username, Localization.REDEEM_NO_ARG);
                        return;
                    }

                    await connection.RedeemStreamerTokenAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
