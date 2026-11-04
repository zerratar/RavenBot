using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Craft : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Craft items with crafting; your character switches to training crafting, and !craft on its own just does that. Use !req <item> to see what an item needs.";
        public override string UsageExample => "!craft 4 rope, !craft status";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The item to craft. Or status to see how long your crafting has left, or cancel to stop it.", "status", "cancel").Required(),
            ChatCommandInput.Create("amount", "How many to craft, before or after the name; leave it out for one.").NotRequired(),
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
                    await connection[cmd].CraftAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
