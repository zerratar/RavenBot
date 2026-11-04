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
        public override string Description => "Enchant an item from your inventory with random bonus stats. You need to be in a clan, and you have to wait a while between tries.";
        public override string UsageExample => "!enchant rune 2h sword, !enchant replace rune 2h sword, !enchant cooldown";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "Leave it out to enchant. replace enchants an item that is already enchanted, remove takes the enchantment off (same as !disenchant), cooldown (or cd) shows how long until you can enchant again, and clear cooldown pays coins to end the wait.", "replace", "remove", "cooldown", "clear cooldown").NotRequired(),
            ChatCommandInput.Create("item", "The item to enchant; not needed with cooldown or clear cooldown.").Required(),
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

                        // games before 0.9.7 took req out of "!enchant rune sword req" and enchanted the sword for real
                        if (System.Array.Exists(part, x => x == "req" || x == "requirement" || x == "requirements")
                            && (connection.ClientVersion == null || RavenBot.Core.GameVersion.IsLessThanOrEquals(connection.ClientVersion, "0.9.6.9")))
                        {
                            await chat.SendReplyAsync(cmd, "Enchanting has no item requirements: you need to be in a clan with the enchanting skill, and each enchant has a cooldown.");
                            return;
                        }

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
