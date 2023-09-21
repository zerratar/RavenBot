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
        public override string Description => "Sell items on the marketplace. See https://www.ravenfall.stream/marketplace";
        public override string UsageExample => "!sell 10 rune 2h sword 55k";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "What item you want to sell").Required(),
            ChatCommandInput.Create("amount", "How many of the said item you want to sell"),
            ChatCommandInput.Create("price", "The amount of coins you want per item").Required()
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
                        await chat.SendReplyAsync(cmd, Localization.TRADE_NO_ARG, cmd.Command);
                        return;
                    }

                    await connection[cmd].SellItemAsync(player, cmd.Arguments);
                }
            }
        }
    }
}