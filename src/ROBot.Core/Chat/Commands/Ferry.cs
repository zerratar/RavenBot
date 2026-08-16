using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Ferry : ChatCommandHandler
    {
        public override string Category => "Sailing";
        public override string Description => "Ferry command is used for getting details about the ferry or using ferry boost scrolls.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "Type of action", "boost", "info")
        };
        public override string UsageExample => "!ferry boost";
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

                    var arg = cmd.Arguments?.ToLower();

                    if (!string.IsNullOrEmpty(arg) && arg.StartsWith("boost"))
                    {
                        await connection[cmd].UseFerryScrollAsync(player);
                        return;
                    }

                    await connection[cmd].GetFerryInfoAsync(player);
                }
            }
        }
    }
}
