using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Chop : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "This command allows you to train woodcutting but to only collect certain wood resource.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "What kind of wood logs you want to collect.").Required(),
        };

        public override string UsageExample => "!chop willow";

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
                    await connection[cmd].ChopAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
