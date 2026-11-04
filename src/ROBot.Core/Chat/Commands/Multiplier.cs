using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Multiplier : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "See the exp multiplier your character gets right now, and where it comes from: your Patreon tier, the global exp boost, the village huts for what you are training, and being rested.";
        public override string UsageExample => "!multiplier";
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
                    await connection[cmd].GetMaxMultiplierAsync(player);
                }
            }
        }
    }
}
