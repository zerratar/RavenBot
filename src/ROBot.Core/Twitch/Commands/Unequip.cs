using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Unequip : TwitchCommandHandler
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
                    if (player == null)
                        return;

                    var item = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(item))
                    {
                        twitch.Broadcast(channel, cmd.Sender.Username, "You have to use !equip <item name> or !equip all for equipping your best items.");
                        return;
                    }

                    await connection.UnequipAsync(player, item);
                }
            }
        }
    }
}
