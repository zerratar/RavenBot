using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Shop : ChatCommandHandler
    {
        public override string Category => "Tavern";
        public override string Description => "Lists the streamer's tavern shelves. The streamer can also take a shelf down, change a price and see what the shelves have made for the village.";
        public override string UsageExample => "!shop, !shop remove 3, !shop price 3 2500, !shop report";
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

                    await connection[cmd].ShopAsync(player, cmd.Arguments ?? string.Empty);
                }
            }
        }
    }
}
