using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Buy : ChatCommandHandler
    {
        public override string Description => "Buy an item from the marketplace, paying at most the price you name for each. See what is for sale at https://www.ravenfall.stream/marketplace";
        public override string UsageExample => "!buy rune 2h sword 1 150k";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The item to buy.").Required(),
            ChatCommandInput.Create("amount", "How many to buy; leave it out for one.").NotRequired(),
            ChatCommandInput.Create("price", "The most coins you will pay for each one, always the last number; 500, 25k and 1.5m all work.").Required()
        };
        public override string Category => "Items";
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

                    await connection[cmd].BuyItemAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
