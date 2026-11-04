using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Inspect : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Get a link to a character's page on the website, showing their skills, equipment and inventory.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("player", "The name of a player in this stream; leave it out to inspect yourself.").NotRequired()
        };
        public override string UsageExample => "!inspect zerratar";
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
                    if (!string.IsNullOrEmpty(cmd.Arguments))
                    {
                        player = session.GetUserByName(cmd.Arguments);
                    }

                    await connection[cmd].InspectPlayerAsync(player);
                }
            }
        }
    }
}
