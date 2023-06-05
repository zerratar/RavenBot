﻿using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Add : ChatCommandHandler
    {
        public override async Task HandleAsync(IBotServer game, IChatCommandClient twitch, ICommand cmd)
        {
            if (!cmd.Sender.DisplayName.ToLower().Equals("zerratar") && !cmd.Sender.IsGameAdmin)
            {
                return;
            }

            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {

                    if (string.IsNullOrEmpty(cmd.Arguments))
                        return;

                    var values = cmd.Arguments.Split(' ');
                    if (values.Length <= 0)
                        return;

                    var player = session.GetUserByName(values[0]);
                    if (player == null)
                        return;

                    if (values.Length > 1)
                        player.Identifier = values[1];

                    await connection[cmd].JoinAsync(player);
                }
            }
        }
    }
}
