﻿using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Pet : TwitchCommandHandler
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
                    if (player == null)
                        return;

                    var pet = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(pet))
                    {
                        await connection.GetPetAsync(player);
                        return;
                    }

                    await connection.SetPetAsync(player, pet);
                }
            }
        }
    }
}
