using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Equipment : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "See the combined stats of everything you have equipped, or the stats of one kind of gear.";
        public override string UsageExample => "!equipment, !equipment weapon, !equipment armor";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "Which gear to look at: weapon (or sword, axe, spear, katana), ranged (or bow), magic (or staff), armor for all the armor you wear together, or shield, amulet, ring or pet. Leave it out for your totals.", "weapon", "ranged", "magic", "armor", "shield", "amulet", "ring", "pet").NotRequired()
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
