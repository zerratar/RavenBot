using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Boat : ChatCommandHandler
    {
        public override string Category => "Sailing";
        public override string Description => "Your own boat: where it is and how fast it sails. !boat build fishing boat (or sloop) has the shipwright on Away build one from crafted parts and coins; !boat list says what each needs.";
        public override string UsageExample => "!boat, !boat build fishing boat, !boat list";
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

                    await connection[cmd].BoatAsync(player, cmd.Arguments ?? string.Empty);
                }
            }
        }
    }
}
