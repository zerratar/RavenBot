using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Unequip : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Unequip command is used for unequipping a target equipped item.";
        public override string UsageExample => "!unequip rune 2h sword";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "Which item you want to unequip").Required()
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
                        await chat.SendReplyAsync(cmd, "You have to use !equip <item name> or !equip all for equipping your best items.");
                        return;
                    }

                    await connection[cmd].UnequipAsync(player, item);
                }
            }
        }
    }
}
