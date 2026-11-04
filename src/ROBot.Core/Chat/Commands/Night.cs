using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Night : ChatCommandHandler
    {
        public override string Category => "Game";
        public override bool RequiresBroadcaster => true;
        public override string Description => "Turn the in-game time to night. Only the streamer and moderators can use it.";
        public override string UsageExample => "!night";

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
                    if (player != null)
                        await connection[cmd].SetTimeOfDayAsync(player, 230, 30);
                }
            }
        }
    }
}
