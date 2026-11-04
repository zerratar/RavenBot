using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Gather : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Train Gathering. Name something to gather only that, or leave it out to gather anything your level allows.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "What to gather, such as mushroom; leave it out to gather anything.").NotRequired(),
        };

        public override string UsageExample => "!gather mushroom";

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
                    await connection[cmd].GatherAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
