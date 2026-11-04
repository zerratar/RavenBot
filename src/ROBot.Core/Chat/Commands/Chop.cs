using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Chop : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Train woodcutting. Name a kind of logs to chop only the trees that give them.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "The logs you want, for example willow; leave it out to chop any tree you can.").NotRequired(),
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
