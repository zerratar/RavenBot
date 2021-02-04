using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Redeem : TwitchCommandHandler
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
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        twitch.Broadcast(channel, cmd.Sender.Username, Localization.REDEEM_NO_ARG);
                        return;
                    }

                    await connection.RedeemStreamerTokenAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
