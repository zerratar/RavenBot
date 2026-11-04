using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Sell : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Put items up for sale on the marketplace for other players to buy with !buy. See https://www.ravenfall.stream/marketplace";
        public override string UsageExample => "!sell 10 rune 2h sword 55k";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The name of the item to sell.").Required(),
            ChatCommandInput.Create("amount", "How many to sell (1 when left out).").NotRequired(),
            ChatCommandInput.Create("price", "The price in coins for each item; k and m work, like 55k.").Required()
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

                    if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
                    {
                        await chat.SendReplyAsync(cmd, Localization.OLD_TRADE_NO_ARG, cmd.Command);
                        return;
                    }

                    await connection[cmd].SellItemAsync(player, cmd.Arguments);
                }
            }
        }
    }
}