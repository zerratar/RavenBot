using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Prepare : Cook { }
    public class Cook : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Cook food with cooking; your character switches to training cooking, and !cook on its own just does that. Use !req <item> to see what a dish needs.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The dish to cook, or a raw ingredient such as shrimps. Or status to see how long your cooking has left, or cancel to stop it.", "status", "cancel").Required(),
            ChatCommandInput.Create("amount", "How many to cook, before or after the name; leave it out for one.").NotRequired(),
        };

        public override string UsageExample => "!cook 10 shrimps";

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
                    await connection[cmd].CookAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
