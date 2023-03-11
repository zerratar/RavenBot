using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Raid : ChatCommandHandler
    {
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

                        await connection.Reply(cmd.CorrelationId).JoinRaidAsync(player, null);
                        return;
                    }

                    if (!string.IsNullOrEmpty(cmd.Arguments))
                    {
                        if (cmd.Arguments.Contains("start", StringComparison.OrdinalIgnoreCase))
                        {
                            await connection.Reply(cmd.CorrelationId).RaidStartAsync(player);
                            return;
                        }

                        if (cmd.Arguments.Contains("stop", StringComparison.OrdinalIgnoreCase))
                        {
                            await connection.Reply(cmd.CorrelationId).StopRaidAsync(player);
                            return;
                        }

                        if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsGameAdmin)
                        {
                            chat.SendReply(cmd, Localization.PERMISSION_DENIED);
                            return;
                        }

                        var target = session.GetUserByName(cmd.Arguments);
                        await connection.Reply(cmd.CorrelationId).RaidStreamerAsync(player, target, isRaidWar);
                    }
                }
            }
        }
    }
}
