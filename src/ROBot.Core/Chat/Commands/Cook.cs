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
        public override string Description => "This command allows you to cook an item, it requires your character to be training cooking to do so.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("req", "Include 'req' if you want to just check the cooking requirement of a recipe/item."),
            ChatCommandInput.Create("item", "target item or recipe you want to cook").Required(),
            ChatCommandInput.Create("amount", "the amount of said item you want to cook"),
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
