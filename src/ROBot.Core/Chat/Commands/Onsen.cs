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
        public override string Description => "Rest at the onsen to build up rested time, which gives you 2x exp afterwards. Onsens are on Kyo, Heim, Atria and Eldara; !onsen leave gets you out.";
        public override string UsageExample => "!onsen leave";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "leave gets out of the onsen; leave it out to start resting.", "leave").NotRequired()
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
