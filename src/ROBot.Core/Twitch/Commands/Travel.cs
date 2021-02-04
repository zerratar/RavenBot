using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Travel : TwitchCommandHandler
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
                    var destination = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(destination))
                    {
                        twitch.Broadcast(channel, cmd.Sender.Username, Localization.TRAVEL_NO_ARG);
                        return;
                    }

                    var player = session.Get(cmd.Sender);
                    if (player != null)
                        await connection.TravelAsync(player, destination);
                }
            }
        }
    }
}
