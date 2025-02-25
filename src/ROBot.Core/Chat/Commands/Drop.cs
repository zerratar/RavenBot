using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Drop : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "Game Administrators can use this to initiate an item drop event.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
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
