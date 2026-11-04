using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Examine : ChatCommandHandler
    {
        public override string Description => "Read an item's description. Leave the item out (or type last) to examine the item you got most recently.";
        public override string UsageExample => "!examine rune 2h sword";
        public override string Category => "Items";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The item's name; leave it out, or type last, for the item you got most recently.")
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
                    await connection[cmd].ExamineItemAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
