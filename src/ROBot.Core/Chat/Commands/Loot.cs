using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Loot : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "List the loot you have found, newest first. Filter it by where it came from, by item name or by time, and combine filters with and or or; !loot clear empties the list.";
        public override string UsageExample => "!loot last raid";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
            {
                ChatCommandInput.Create("filter", "raid or dungeon for all raid or dungeon loot, last raid or last dungeon for the latest one, part of an item name (sword), a time span (10 minutes) for loot found since then, or clear to empty the list; leave it out for everything.",
                    "raid", "dungeon", "last raid", "last dungeon", "clear").NotRequired(),
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
                    await connection[cmd].GetLootAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
