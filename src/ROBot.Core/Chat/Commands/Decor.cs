using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Decor : ChatCommandHandler
    {
        public override string Category => "Village";
        public override string Description => "Give the streamer's village a decor piece with your loyalty points; the streamer places it in build mode. !decor price shows what a piece costs.";
        public override string UsageExample => "!decor gift snowman, !decor price fountain";
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
                    if (player == null)
                        return;

                    await connection[cmd].DecorAsync(player, cmd.Arguments ?? string.Empty);
                }
            }
        }
    }
}
