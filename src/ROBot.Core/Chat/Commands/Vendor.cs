using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Vendor : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Sell an item to the vendor. (Note: this command will be replaced with !vendor buy|sell|value later)";
        public override string UsageExample => "!vendor rune 2h sword";

        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "What kind of interaction?", /*"buy",*/ "sell", "value").Required(),
            ChatCommandInput.Create("item", "The target item you want to vendor").Required(),
            ChatCommandInput.Create("amount", "The amount of the specified item you want to vendor")
        };

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
            if (player == null)
                return;

            if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Contains(' '))
                return;

            var action = cmd.Arguments.Split(' ')[0].ToLower();
            if (action.Equals("value") || action.Equals("buy") || action.Equals("sell"))
            {
                await connection[cmd].UseVendorAsync(player, cmd.Arguments);
                return;
            }

            await connection[cmd].VendorItemAsync(player, cmd.Arguments);
        }
    }
}
