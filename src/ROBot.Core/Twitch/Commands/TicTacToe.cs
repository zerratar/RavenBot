﻿using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class TicTacToe : TwitchCommandHandler
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
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await connection.ActivateTicTacToeAsync(player);
                        return;
                    }

                    if (cmd.Arguments.Trim().Equals("reset", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await connection.ResetTicTacToeAsync(player);
                        return;
                    }

                    if (int.TryParse(cmd.Arguments.Trim(), out var num))
                    {
                        await connection.PlayTicTacToeAsync(player, num);
                    }
                }
            }
        }
    }
}
