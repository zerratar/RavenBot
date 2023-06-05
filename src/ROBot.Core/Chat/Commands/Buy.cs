using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Buy : ChatCommandHandler
    {
        public override string Description => "Buy items from the marketplace. See https://www.ravenfall.stream/marketplace";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "What item you want to buy").Required(),
            ChatCommandInput.Create("amount", "How many of the said item you want to buy"),
            ChatCommandInput.Create("price", "The maximum amount of coins you want to pay per item").Required()
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
                        chat.SendReply(cmd, Localization.TRADE_NO_ARG, cmd.Command);
                        return;
                    }

                    await connection[cmd].BuyItemAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
