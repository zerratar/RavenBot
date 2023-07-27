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
        public override string Description => "Gets the current exp multiplier that your character is affected by. This includes patreon boost, global exp multiplier, hut boost and rested.";
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
