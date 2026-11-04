using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Exp : ChatCommandHandler
    {
        public override string Description => "Use your Exp Scrolls to raise the global exp multiplier for everyone in the stream: each scroll adds 1, up to the stream's multiplier limit.";
        public override string UsageExample => "!exp 10";

        public override string Category => "Game";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("amount", "How many Exp Scrolls to use; leave it out for one.").NotRequired(),
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
