using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Uses : Usage { }
    public class Usage : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Shows which recipes an item is used in.";
        public override string UsageExample => "!usage iron ore";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The name of the item to look up.").Required(),
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
                    await connection[cmd].GetItemUsageAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
