using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Sail : TwitchCommandHandler
    {
        public override async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var player = session.Get(cmd.Sender);

                    var destination = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(destination))
                    {
                        await connection.EmbarkFerryAsync(player);
                        return;
                    }

                    if (destination.StartsWith("stop"))
                    {
                        await connection.DisembarkFerryAsync(player);
                        return;
                    }

                    await connection.TravelAsync(player, destination);
                }
            }
        }
    }
}
