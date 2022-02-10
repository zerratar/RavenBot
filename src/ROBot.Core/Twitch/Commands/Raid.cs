using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Raid : TwitchCommandHandler
    {
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
                    var isRaidWar = cmd.Command.Contains("war", StringComparison.OrdinalIgnoreCase);
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        if (isRaidWar)
                        {
                            return;
                        }

                        await connection.JoinRaidAsync(new EventJoinRequest(player, null));
                        return;
                    }

                    if (!string.IsNullOrEmpty(cmd.Arguments))
                    {
                        if (cmd.Arguments.Contains("start", StringComparison.OrdinalIgnoreCase))
                        {
                            await connection.RaidStartAsync(player);
                            return;
                        }

                        if (cmd.Arguments.Contains("stop", StringComparison.OrdinalIgnoreCase))
                        {
                            await connection.StopRaidAsync(player);
                            return;
                        }

                        if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsGameAdmin)
                        {
                            twitch.Broadcast(channel, cmd.Sender.Username, Localization.PERMISSION_DENIED);
                            return;
                        }

                        var target = session.GetUserByName(cmd.Arguments);
                        await connection.RaidStreamerAsync(target, isRaidWar);
                    }
                }
            }
        }
    }
}
