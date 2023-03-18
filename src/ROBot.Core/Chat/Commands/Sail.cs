using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Sail : ChatCommandHandler
    {
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

                    var destination = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(destination))
                    {
                        await connection[cmd.CorrelationId].EmbarkFerryAsync(player);
                        return;
                    }

                    if (destination.StartsWith("stop"))
                    {
                        await connection[cmd.CorrelationId].DisembarkFerryAsync(player);
                        return;
                    }

                    await connection[cmd.CorrelationId].TravelAsync(player, destination);
                }
            }
        }
    }
}
