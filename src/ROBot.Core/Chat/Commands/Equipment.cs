using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Equipment : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Get the current stats of your equipped items, or more details of a specific one.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "Details about the equipment", "shield", "weapon", "ranged", "magic", "armor", "amulet", "ring", "pet")
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
                    await connection[cmd].RequestPlayerEquipmentStatsAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
