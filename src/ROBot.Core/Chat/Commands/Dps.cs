using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Dps : ChatCommandHandler
    {
        public override string Description => "See your damage and healing per second this session, with your highest hit, highest heal and totals.";
        public override string UsageExample => "!dps";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>();
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
                    await connection[cmd].GetDpsAsync(player);
                }
            }
        }
    }

    public class Damage : Dps { }
    public class Dmg : Dps { }
}
