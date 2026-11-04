using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Drop : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "Game admins only: starts an item drop event with two items for every player in the game.";
        public override string UsageExample => "!drop random";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The item to drop, random for random items, or hats for random hats.", "random", "hats").Required(),
        };

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session == null)
            {
                return;
            }

            var connection = game.GetConnection(session);
            if (connection == null)
            {
                return;
            }

            var player = session.Get(cmd);
            if (player == null || !player.IsGameAdministrator)
            {
                return;
            }

            await connection[cmd].ItemDropEventAsync(player, cmd.Arguments?.Trim());
        }
    }
}
