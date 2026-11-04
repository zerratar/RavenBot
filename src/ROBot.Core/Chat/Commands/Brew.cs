using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Conjure : Brew { }
    public class Alchemise : Brew { }
    public class Brew : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Brew potions with alchemy; your character switches to training alchemy, and !brew on its own just does that. Use !req <item> to see what a potion needs.";
        public override string UsageExample => "!brew 5 health potion, !brew status";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The potion to brew. Or status to see how long your brewing has left, or cancel to stop it.", "status", "cancel").Required(),
            ChatCommandInput.Create("amount", "How many to brew, before or after the name; leave it out for one.").NotRequired(),
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
                    await connection[cmd].BrewAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
