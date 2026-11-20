using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Use : ChatCommandHandler
    {
        public override string Description => "Use an item from your inventory: drink a potion, eat food, use a scroll (an exp scroll boosts everyone's exp, a raid or dungeon scroll starts one), or read an experience book for a skill (!use gathering book fishing).";
        public override string UsageExample => "!use health potion";
        public override string Category => "Items";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The name of the item to use, like health potion or exp scroll. For an experience book (quests give one in place of exp for a skill at level 999), its kind and the skill it goes to: combat book magic, gathering book fishing, artisan book cooking, adventure book taming.").Required(),
            ChatCommandInput.Create("amount", "For exp scrolls and experience books: how many to use at once, or max.").NotRequired()
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
                    await connection[cmd].UseItemAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
