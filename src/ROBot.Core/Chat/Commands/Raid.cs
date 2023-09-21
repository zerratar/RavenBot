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
        public override string Description => "This command allows you to join a raid, use a raid scroll, forcibly stop a raid or start a streamer raid.";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "Leave empty if you intend to join a raid, 'stop' to forcibly stop or 'start' to use a raid scroll", "start", "stop"),
            ChatCommandInput.Create("target", "The target user to raid. Only broadcaster can use this."),
        };
        public override string UsageExample => "!raid start";
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
                        if (cmd.Arguments.Contains("join ", System.StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd].AutoJoinRaidAsync(player, cmd.Arguments.Split(' ').LastOrDefault());
                            return;
                        }

                        if (cmd.Arguments.Contains("start", StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd].RaidStartAsync(player);
                            return;
                        }

                        if (cmd.Arguments.Contains("stop", StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd].StopRaidAsync(player);
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
