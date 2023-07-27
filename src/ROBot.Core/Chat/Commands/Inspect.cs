using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Inspect : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Inspect command is used for getting an inspection link of a character, that will show what stats, inventory, etc the character has.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "Who do you want to inspect? (Leave empty for yourself)")
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
