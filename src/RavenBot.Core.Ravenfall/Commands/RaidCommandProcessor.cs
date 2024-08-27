using System;
using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class RaidCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        private readonly ITwitchUserStore userStore;
        public RaidCommandProcessor(IRavenfallClient game, IUserProvider playerProvider, ITwitchUserStore userStore)
        {
            this.game = game;
            this.playerProvider = playerProvider;
            this.userStore = userStore;
        }
        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);
            var isRaidWar = cmd.Command.Contains("war", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                if (isRaidWar)
                {
                    return;
                }

                await this.game[cmd.CorrelationId].JoinRaidAsync(player, null);
                return;
            }

            if (!string.IsNullOrEmpty(cmd.Arguments))
            {
                if (cmd.Arguments.Contains("join ", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("auto ", System.StringComparison.OrdinalIgnoreCase))
                {
                    await game[cmd.CorrelationId].AutoJoinRaidAsync(player, cmd.Arguments.Split(' ').LastOrDefault());
                    return;
                }
                else if (cmd.Arguments.Contains("start", StringComparison.OrdinalIgnoreCase))
                {
                    await this.game[cmd.CorrelationId].RaidStartAsync(player);
                    return;
                }
                else if (cmd.Arguments.Contains("kill boss", StringComparison.OrdinalIgnoreCase))
                {
                    await this.game[cmd].KillRaidBossAsync(player);
                    return;
                }
                else if (cmd.Arguments.Contains("stop", StringComparison.OrdinalIgnoreCase))
                {
                    await this.game[cmd.CorrelationId].StopRaidAsync(player);
                    return;
                }
                else if (cmd.Arguments.Equals("skill", System.StringComparison.OrdinalIgnoreCase))
                {
                    await this.game[cmd.CorrelationId].GetRaidCombatStyleAsync(player);
                    return;
                }
                else if (cmd.Arguments.Contains("skill ", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("style ", System.StringComparison.OrdinalIgnoreCase))
                {
                    var targetSkill = cmd.Arguments.Split(' ').Skip(1).FirstOrDefault();
                    if (targetSkill.Equals("reset", System.StringComparison.OrdinalIgnoreCase) || targetSkill.Equals("clear", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await game[cmd].ClearRaidCombatStyleAsync(player);
                    }
                    else
                    {
                        await game[cmd].SetRaidCombatStyleAsync(player, targetSkill);
                    }
                    return;
                }

                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsGameAdmin)
                {
                    await chat.SendReplyAsync(cmd, Localization.PERMISSION_DENIED);
                    return;
                }

                var sender = playerProvider.Get(cmd);
                var target = playerProvider.Get(cmd.Arguments);
                await this.game[cmd.CorrelationId].RaidStreamerAsync(sender, target, isRaidWar);
            }
        }
    }
}