using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Raid : ChatCommandHandler
    {
        public override string Description => "Join the raid boss fight with !raid, start one with a raid scroll, set up auto join or pick the combat skill you train in raids. The streamer can also raid another stream with its players.";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action",
                "Leave empty to join the raid. " +
                "start uses one of your raid scrolls to start a raid. " +
                "stop ends the raid, for the streamer and moderators only. " +
                "join (or auto) sets up auto join. " +
                "skill shows or sets the combat skill you train in raids. " +
                "The streamer can type another streamer's name instead to raid their stream.",
                "start", "stop", "join", "skill").NotRequired(),
            ChatCommandInput.Create("value",
                "join: on, off, status or a number of raids to join. " +
                "skill: a combat skill (attack, defense, strength, ranged, magic, healing), or reset to go back to your normal training; leave empty to see yours.").NotRequired(),
        };
        public override string UsageExample => "!raid join on";
        public override string Category => "Events";
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
                    var isRaidWar = cmd.Command.Contains("war", StringComparison.OrdinalIgnoreCase);
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        if (isRaidWar)
                        {
                            return;
                        }

                        await connection[cmd].JoinRaidAsync(player, null);
                        return;
                    }

                    if (!string.IsNullOrEmpty(cmd.Arguments))
                    {
                        if (cmd.Arguments.Contains("join ", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("auto ", System.StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd].AutoJoinRaidAsync(player, cmd.Arguments.Split(' ').LastOrDefault());
                            return;
                        }
                        else if (cmd.Arguments.Contains("start", StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd].RaidStartAsync(player);
                            return;
                        }
                        else if (cmd.Arguments.Contains("stop", StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd].StopRaidAsync(player);
                            return;
                        }
                        else if (cmd.Arguments.Contains("kill boss", System.StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd].KillRaidBossAsync(player);
                            return;
                        }
                        else if (cmd.Arguments.Equals("skill", System.StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd].GetRaidCombatStyleAsync(player);
                            return;
                        }
                        else if (cmd.Arguments.Contains("skill ", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("style ", System.StringComparison.OrdinalIgnoreCase))
                        {
                            var targetSkill = cmd.Arguments.Split(' ').Skip(1).FirstOrDefault();
                            if (targetSkill.Equals("reset", System.StringComparison.OrdinalIgnoreCase) || targetSkill.Equals("clear", System.StringComparison.OrdinalIgnoreCase))
                            {
                                await connection[cmd].ClearRaidCombatStyleAsync(player);
                            }
                            else
                            {
                                await connection[cmd].SetRaidCombatStyleAsync(player, targetSkill);
                            }
                            return;
                        }

                        if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsGameAdmin)
                        {
                            await chat.SendReplyAsync(cmd, Localization.PERMISSION_DENIED);
                            return;
                        }

                        var target = session.GetUserByName(cmd.Arguments);
                        await connection[cmd].RaidStreamerAsync(player, target, isRaidWar);
                    }
                }
            }
        }
    }
}
