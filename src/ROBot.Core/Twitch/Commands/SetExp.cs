﻿using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class SetExp : TwitchCommandHandler
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

                    var numOfSubs = 1;
                    if (!string.IsNullOrEmpty(cmd.Arguments))
                        int.TryParse(cmd.Arguments, out numOfSubs);

                    await connection.SetExpMultiplierAsync(player, numOfSubs);
                }
            }
        }
    }
}
