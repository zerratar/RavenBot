using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Diaper : ChatCommandHandler
    {
        public override string Category => "Appearance";
        public override string Description => "Diaper mode: takes off all your armor so your character runs around in their bare trousers. Use it again to put your armor back on.";
        public override string UsageExample => "!diaper";
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
                    await connection[cmd].ToggleDiaperModeAsync(player);
                }
            }
        }
    }
}
