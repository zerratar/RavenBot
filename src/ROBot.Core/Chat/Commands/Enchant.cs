using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Enchant : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "This command allows you enchant an item. You have to be part of a clan to use this command.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "target item you want to enchant").Required(),
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

                    if (!string.IsNullOrEmpty(item))
                    {
                        if (item == "remove cooldown" || item == "clear cooldown" || item == "remove cd" || item == "clear cd")
                        {
                            await connection[cmd].ClearEnchantmentCooldownAsync(player);
                            return;
                        }

                        if (item == "cooldown" || item == "cd")
                        {
                            await connection[cmd].GetEnchantmentCooldownAsync(player);
                            return;
                        }

                        var part = item.Split(' ');
                        if (part[0] == "remove")
                        {
                            await connection[cmd].DisenchantAsync(player, item.Replace("remove", "").Trim());
                            return;
                        }
                    }
                    //if (string.IsNullOrEmpty(item))
                    //{
                    //    twitch.Broadcast(channel, cmd.Sender.Username, "You have to use !enchant <item name> to enchant an item.");
                    //    return;
                    //}

                    await connection[cmd].EnchantAsync(player, item);
                }
            }
        }
    }
}
