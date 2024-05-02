using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Market : ChatCommandHandler
    {
        public override string Description => "This command allows for interaction with the marketplace. See https://www.ravenfall.stream/marketplace";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "What kind of interaction?", "buy", "sell", "value").Required(),
            ChatCommandInput.Create("item", "What item").Required(),
            ChatCommandInput.Create("amount", "How many of the said item you want to sell"),
            ChatCommandInput.Create("price", "The amount of coins you want per item, (required for sell)")
        };

        public override string UsageExample => "!market buy rune 2h sword 55k";
        public override string Category => "Items";
        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session == null)
                return;

            var connection = game.GetConnection(session);
            if (connection == null)
                return;

            var player = session.Get(cmd);
            if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
            {
                await chat.SendReplyAsync(cmd, Localization.MARKET_TRADE_NO_ARG, cmd.Command);
                return;
            }

            var action = cmd.Arguments.Split(' ')[0].ToLower();
            if (action != "buy" && action != "sell" && action != "value")
            {
                await chat.SendReplyAsync(cmd, Localization.MARKET_TRADE_INVALID_ACTION, cmd.Command);
                return;
            }

            await connection[cmd].UseMarketAsync(player, cmd.Arguments);
        }
    }
}
