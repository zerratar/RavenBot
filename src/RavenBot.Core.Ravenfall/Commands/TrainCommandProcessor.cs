﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RavenBot.Core.Extensions;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class TrainCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        private readonly string[] trainableSkills = new string[]
        {
            "all", "atk", "def", "str", "magic",
            "ranged", "fishing", "cooking", "woodcutting",
            "mining", "crafting", "farming", "healing", "gathering", "alchemy"
        };

        public TrainCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }
            var player = playerProvider.Get(cmd);


            if (GetCombatTypeFromString(cmd.Command) != -1)
            {
                await this.game[cmd.CorrelationId].SendPlayerTaskAsync(player, PlayerTask.Fighting, cmd.Command);
                return;
            }
            var commandSkillTarget = GetSkillTypeFromString(cmd.Command);
            if (commandSkillTarget != -1)
            {
                await this.game[cmd.CorrelationId].SendPlayerTaskAsync(player, (PlayerTask)commandSkillTarget, cmd.Command);
                return;
            }

            var arg = cmd.Arguments?.ToLower();
            var arguments = arg?.Split(' ');
            arguments = GetTargetLevelFromArguments(arguments, out var levelTarget);

            var skill = GetSkillFromArguments(arguments);

            if (string.IsNullOrEmpty(skill))
            {
                await chat.SendReplyAsync(cmd, Localization.TRAIN_NO_ARG, string.Join(", ", trainableSkills));
                return;
            }

            if (skill.ToLower().StartsWith("slay") || arg.StartsWith("slay"))
            {
                await chat.SendReplyAsync(cmd, "To train Slayer you need to join raids and dungeons.");
                return;
            }

            if (skill.ToLower().StartsWith("sail") || arg.StartsWith("sail"))
            {
                await this.game[cmd.CorrelationId].EmbarkFerryAsync(player);
                return;
            }

            if (GetCombatTypeFromString(skill) != -1)
            {
                if (levelTarget != null)
                {
                    await this.game[cmd.CorrelationId].SendPlayerTaskAsync(player, PlayerTask.Fighting, skill, levelTarget.ToString());
                }
                else
                {
                    await this.game[cmd.CorrelationId].SendPlayerTaskAsync(player, PlayerTask.Fighting, skill);
                }
            }
            else
            {
                var value = GetSkillTypeFromString(skill);
                if (value == -1)
                {
                    await chat.SendReplyAsync(cmd, Localization.TRAIN_INVALID, skill);
                }
                else
                {
                    if (levelTarget != null)
                    {
                        await this.game[cmd.CorrelationId].SendPlayerTaskAsync(player, (PlayerTask)value, skill, levelTarget.ToString());
                    }
                    else
                    {
                        await this.game[cmd.CorrelationId].SendPlayerTaskAsync(player, (PlayerTask)value, skill);
                    }
                }
            }
        }

        private string[] GetTargetLevelFromArguments(string[] arguments, out int? levelTarget)
        {
            levelTarget = null;
            if (arguments == null || arguments.Length < 2)
            {
                return arguments;
            }

            var num = arguments.FirstOrDefault(x => int.TryParse(x, out _));
            if (string.IsNullOrEmpty(num))
            {
                return arguments;
            }

            levelTarget = int.Parse(num);

            return arguments.WhereNot(x => x == num).ToArray();
        }

        private string GetSkillFromArguments(string[] arguments)
        {
            if (arguments == null || arguments.Length < 1)
            {
                return null;
            }

            var skill = arguments.FirstOrDefault(x => GetCombatTypeFromString(x) != -1 || GetSkillTypeFromString(x) != -1);
            if (string.IsNullOrEmpty(skill))
            {
                return arguments[0];
            }

            return skill;
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
            if (StartsWith(val, "gath")) return (int)PlayerTask.Gathering;
            if (StartsWith(val, "alch") || StartsWith(val, "brew")) return (int)PlayerTask.Brewing;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool StartsWith(string str, string arg) => str.StartsWith(arg, StringComparison.OrdinalIgnoreCase);
    }
}