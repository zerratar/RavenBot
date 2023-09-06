using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Teleport : ChatCommandHandler
    {
        public override string Description => "This command allows for using a tome to teleport the player to a target island";
        public override string UsageExample => "!teleport kyo";
        public override string Category => "Character";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("island", "Which island do you wish to teleport to?").Required()
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
