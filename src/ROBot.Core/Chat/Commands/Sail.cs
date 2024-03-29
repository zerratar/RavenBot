﻿using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Sail : ChatCommandHandler
    {
        public override string Category => "Sailing";
        public override string Description => "Sail command is used for sailing between the different islands in game.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("destination", "Where do you want to sail? (Leave empty to train sailing)", "Home", "Away", "Ironhill", "Kyo", "Heim")
        };

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

                    var destination = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(destination))
                    {
                        await connection[cmd].EmbarkFerryAsync(player);
                        return;
                    }

                    if (destination.StartsWith("stop"))
                    {
                        await connection[cmd].DisembarkFerryAsync(player);
                        return;
                    }

                    await connection[cmd].TravelAsync(player, destination);
                }
            }
        }
    }
}
