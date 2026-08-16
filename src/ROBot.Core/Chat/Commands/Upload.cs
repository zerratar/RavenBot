using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Upload : ChatCommandHandler
    {
        public override string Description => "Use this command to trigger a log or game state upload.";
        public override string UsageExample => "!upload log";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("option", "What to upload to the server. ('state' is default if value left empty)", "log", "state").Required()
        };

        public override string Category => "Game";
        public override bool RequiresBroadcaster => true;

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
                    var type = cmd.Arguments?.Trim().ToLowerInvariant() ?? "state";
                    await connection[cmd].UploadAsync(player, type);
                }
            }
        }
    }

}
