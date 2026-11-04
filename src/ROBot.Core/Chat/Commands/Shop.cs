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
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action",
                "Leave empty (or list) to see the shelves; buy from one with !redeem and its shelf number. " +
                "For the streamer only: remove takes a shelf down, price changes a shelf's price and report shows what the shelves have made.",
                "list", "remove", "price", "report").NotRequired(),
            ChatCommandInput.Create("shelf", "remove and price: the shelf's number in !shop.").NotRequired(),
            ChatCommandInput.Create("price", "price: the new price in coins for each item.").NotRequired(),
        };
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
