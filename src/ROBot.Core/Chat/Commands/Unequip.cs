using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Unequip : ChatCommandHandler
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
                    if (player == null)
                        return;

                    var item = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(item))
                    {
                        chat.SendReply(cmd, "You have to use !equip <item name> or !equip all for equipping your best items.");
                        return;
                    }

                    await connection.Reply(cmd.CorrelationId).UnequipAsync(player, item);
                }
            }
        }
    }
}
