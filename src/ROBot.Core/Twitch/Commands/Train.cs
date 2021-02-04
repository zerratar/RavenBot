using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.GameServer;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Train : TwitchCommandHandler
    {
        private readonly string[] trainableSkills = new string[]
        {
            "all", "atk", "def", "str", "magic",
            "ranged", "fishing", "cooking", "mining",
            "crafting", "farming", "healing",
        };

        public override async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var player = session.Get(cmd.Sender);

                    if (GetCombatTypeFromString(cmd.Command) != -1)
                    {
                        await connection.SendPlayerTaskAsync(player, PlayerTask.Fighting, cmd.Command);
                        return;
                    }

                    var commandSkillTarget = GetSkillTypeFromString(cmd.Command);
                    if (commandSkillTarget != -1)
                    {
                        await connection.SendPlayerTaskAsync(player, (PlayerTask)commandSkillTarget, cmd.Command);
                        return;
                    }

                    var arg = cmd.Arguments?.ToLower();
                    var skill = arg?.Split(' ').LastOrDefault();
                    if (string.IsNullOrEmpty(skill))
                    {
                        twitch.Broadcast(channel, cmd.Sender.Username, Localization.TRAIN_NO_ARG, string.Join(", ", trainableSkills));
                        return;
                    }

                    if (GetCombatTypeFromString(skill) != -1)
                    {
                        await connection.SendPlayerTaskAsync(player, PlayerTask.Fighting, skill);
                    }
                    else
                    {
                        var value = GetSkillTypeFromString(skill);
                        if (value == -1)
                        {
                            twitch.Broadcast(channel, cmd.Sender.Username, Localization.TRAIN_INVALID, skill);
                        }
                        else
                        {
                            await connection.SendPlayerTaskAsync(player, (PlayerTask)value, skill);
                        }
                    }
                }
            }
        }


        public int GetCombatTypeFromString(string val)
        {
            if (StartsWith(val, "atk") || StartsWith(val, "att")) return 0;
            if (StartsWith(val, "def")) return 1;
            if (StartsWith(val, "str")) return 2;
            if (StartsWith(val, "all")) return 3;
            if (StartsWith(val, "magic")) return 4;
            if (StartsWith(val, "ranged")) return 5;
            if (StartsWith(val, "heal")) return 6;
            return -1;
        }

        public int GetSkillTypeFromString(string val)
        {
            if (StartsWith(val, "wood") || StartsWith(val, "chop") || StartsWith(val, "wdc") || StartsWith(val, "chomp")) return (int)PlayerTask.Woodcutting;
            if (StartsWith(val, "fish") || StartsWith(val, "fsh") || StartsWith(val, "fist")) return (int)PlayerTask.Fishing;
            if (StartsWith(val, "cook") || StartsWith(val, "ckn")) return (int)PlayerTask.Cooking;
            if (StartsWith(val, "craft")) return (int)PlayerTask.Crafting;
            if (StartsWith(val, "mine") || StartsWith(val, "min") || StartsWith(val, "mining")) return (int)PlayerTask.Mining;
            if (StartsWith(val, "farm") || StartsWith(val, "fm")) return (int)PlayerTask.Farming;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool StartsWith(string str, string arg) => str.StartsWith(arg, StringComparison.OrdinalIgnoreCase);
    }
}
