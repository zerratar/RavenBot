using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Ravenfall.Models;
using RavenBot.Core.Twitch;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Show : TwitchCommandHandler
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
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsSubscriber)
                    {
                        twitch.Broadcast(channel, cmd.Sender.Username, Localization.OBSERVE_PERM);
                        return;
                    }

                    var targetPlayerName = cmd.Arguments?.Trim();
                    var player = string.IsNullOrEmpty(targetPlayerName)
                        ? session.Get(cmd.Sender)
                        : session.GetUserByName(targetPlayerName);

                    await connection.ObservePlayerAsync(player);
                }
            }
        }
    }
}
