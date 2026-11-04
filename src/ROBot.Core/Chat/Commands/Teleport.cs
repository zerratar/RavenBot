using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Teleport : ChatCommandHandler
    {
        public override string Description => "Use a Teleportation Tome from your inventory to teleport straight to an island. You cannot teleport during a raid, dungeon, duel, arena fight, raid war or on Ravebridge.";
        public override string UsageExample => "!teleport kyo";
        public override string Category => "Character";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("island", "The island to teleport to. You need a tome for that island.", "Home", "Away", "Ironhill", "Kyo", "Heim", "Atria", "Eldara").Required()
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
                    await connection[cmd].TeleportAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
