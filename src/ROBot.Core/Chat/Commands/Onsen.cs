using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Onsen : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "Onsen command is used for making your character rest to gain 2x more exp for the rested time. This only works on Heim and Kyo islands.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("option", "Start resting or stop resting?", "exit")
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

                    var leaveOnsen = !string.IsNullOrEmpty(cmd.Arguments) && cmd.Arguments.Contains("leave", StringComparison.OrdinalIgnoreCase);
                    if (leaveOnsen)
                    {
                        await connection[cmd].LeaveOnsenAsync(player);
                    }
                    else
                    {
                        await connection[cmd].JoinOnsenAsync(player);
                    }
                }
            }
        }
    }
}
