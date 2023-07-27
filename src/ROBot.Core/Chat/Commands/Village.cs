﻿using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Village : ChatCommandHandler
    {
        public override string Description => "This command allows getting or setting details about the village.";
        public override string UsageExample => "!village melee";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("skill", "Set all available huts to a target skill allowing for quick update", "melee", "magic", "ranged", "healing", "woodcutting", "mining", "fishing", "sailing", "farming"),
        };
        public override string Category => "Game";
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
                    if (!string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await connection[cmd].SetAllVillageHutsAsync(player, cmd.Arguments);
                    }
                    else
                    {
                        await connection[cmd].GetVillageBoostAsync(player);
                    }
                }
            }
        }
    }
}
