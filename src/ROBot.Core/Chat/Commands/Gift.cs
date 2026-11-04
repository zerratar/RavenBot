using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Gift : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Give items or coins from your inventory to another player. The player's name always comes first; the item and the amount can come in either order. Add set to give a whole item set, such as !gift zerratar rune set.";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("player", "The name of the player to give to. It must come first.").Required(),
            ChatCommandInput.Create("item", "The item to give, coins to give coins (!gift zerratar coins 5000 or !gift zerratar 5000 coins), or an item set followed by the word set (rune set). Soulbound items cannot be given.").Required(),
            ChatCommandInput.Create("amount", "How many to give, or how many of each piece for a set; leave it out for one. Shorthands like 10k or 2m work.").NotRequired(),
        };

        public override string UsageExample => "!gift zerratar 1k burned shrimps";
        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
                    {
                        await chat.SendReplyAsync(cmd, Localization.GIFT_HELP, cmd.Command);
                        return;
                    }

                    // "!gift zerratar 5000 coins" looked for an item called "5000 coins" in games before 0.9.7: coins first
                    var args = cmd.Arguments.Trim();
                    var words = args.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 3 && (words[2].Equals("coins", System.StringComparison.OrdinalIgnoreCase) || words[2].Equals("coin", System.StringComparison.OrdinalIgnoreCase)))
                        args = words[0] + " coins " + words[1];

                    var player = session.Get(cmd);
                    if (player != null)
                        await connection[cmd].GiftItemAsync(player, args);
                }
            }
        }
    }
}
