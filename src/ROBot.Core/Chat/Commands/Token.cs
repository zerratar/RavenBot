using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Token : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Gets the amount of different seasonal tokens you have, (halloween, christmas, easter, new year, birthday, etc)";
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
                    await connection[cmd].GetStreamerTokenCountAsync(player);
                }
            }
        }
    }
}
