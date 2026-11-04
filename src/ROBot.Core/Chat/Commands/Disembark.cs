using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Disembark : ChatCommandHandler
    {
        public override string Category => "Sailing";
        public override string Description => "Get off the ferry at its next stop, or sail your own boat to the nearest dock and land there. A guest on someone else's boat goes ashore at the nearest dock.";
        public override string UsageExample => "!disembark";
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
                    await connection[cmd].DisembarkFerryAsync(player);
                }
            }
        }
    }
}
