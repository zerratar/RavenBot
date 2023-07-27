using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Exp : ChatCommandHandler
    {
        public override string Description => "Use a Exp Scroll to increase the global exp multiplier, 1 scroll increases the multiplier by 1";
        public override string UsageExample => "!exp 10";

        public override string Category => "Game";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("amount", "The amount of exp scrolls you want to use, default is 1."),
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

                    var numOfSubs = 1;
                    if (!string.IsNullOrEmpty(cmd.Arguments))
                        int.TryParse(cmd.Arguments, out numOfSubs);

                    await connection[cmd].UseExpMultiplierScrollAsync(player, numOfSubs);
                }
            }
        }
    }
}
