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
        public override string Description => "Sell items from your inventory to the vendor for coins right away. See what the vendor pays with !value; to sell to other players use !sell. !vendor sell and !vendor value work too.";
        public override string UsageExample => "!vendor 10 iron ore";

        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The name of the item to sell to the vendor.").Required(),
            ChatCommandInput.Create("amount", "How many to sell (1 when left out).").NotRequired()
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

            // a single word is an item too: !vendor coal was dropped here
            if (string.IsNullOrWhiteSpace(cmd.Arguments))
                return;

            var action = cmd.Arguments.Trim().Split(' ')[0].ToLower();
            if (action.Equals("value") || action.Equals("buy") || action.Equals("sell"))
            {
                await connection[cmd].UseVendorAsync(player, cmd.Arguments);
                return;
            }

            await connection[cmd].VendorItemAsync(player, cmd.Arguments);
        }
    }
}
