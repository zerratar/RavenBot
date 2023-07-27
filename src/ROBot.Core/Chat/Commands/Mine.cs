﻿using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Mine : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "This command allows you to train mining but to only collect certain resource.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "What kind of ore you want to collect.").Required(),
        };

        public override string UsageExample => "!mine iron ore";

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
                    await connection[cmd].MineAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
