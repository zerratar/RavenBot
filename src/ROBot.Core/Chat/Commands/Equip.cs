using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Equip : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Equip an item, your best items (!equip all), or a role's class gear (!equip tank, ranger, mage).";
        public override string UsageExample => "!equip rune 2h sword";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The item to equip, all for your best items, or a role or class gear line (tank, ranger, rogue).", "all").Required()
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
                    if (player == null)
                        return;

                    var item = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(item))
                    {

                        await chat.SendReplyAsync(cmd, "You have to use !equip <item name>, !equip all for your best items, or a role for its class gear: !equip tank, melee, ranger, mage or healer.");
                        return;
                    }

                    await connection[cmd].EquipAsync(player, item);
                }
            }
        }
    }
}
