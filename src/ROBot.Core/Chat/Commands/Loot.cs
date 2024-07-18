using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Loot : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "This command allows you to get a list of loot the player has found.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
            {
                ChatCommandInput.Create("filter", "optional filter for determing which loot to display."),
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
