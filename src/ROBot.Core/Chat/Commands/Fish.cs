using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Fish : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Train Fishing. Name a fish to catch only that one, or leave it out to catch anything your level allows. Out at sea in your own boat you fish over the side, and some fish only bite there.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("fish", "The fish to catch, such as shrimp; leave it out to catch anything.").NotRequired(),
        };

        public override string UsageExample => "!fish shrimp";

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
                    await connection[cmd].FishAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
