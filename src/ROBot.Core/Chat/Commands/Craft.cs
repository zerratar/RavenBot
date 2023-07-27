using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Craft : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "This command allows you to craft an item, it requires your character to be training crafting to do so.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("req", "Include 'req' if you want to just check the crafting requirement of a recipe/item."),
            ChatCommandInput.Create("item", "target item you want to craft").Required(),
            ChatCommandInput.Create("amount", "the amount of said item you want to craft"),
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
