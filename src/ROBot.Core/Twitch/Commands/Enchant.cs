using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Enchant : TwitchCommandHandler
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

                    if (!string.IsNullOrEmpty(item) && item.Split(' ')[0] == "remove")
                    {
                        await connection.DisenchantAsync(player, item.Replace("remove", "").Trim());
                        return;
                    }
                    //if (string.IsNullOrEmpty(item))
                    //{
                    //    twitch.Broadcast(channel, cmd.Sender.Username, "You have to use !enchant <item name> to enchant an item.");
                    //    return;
                    //}

                    await connection.EnchantAsync(player, item);
                }
            }
        }
    }
}
