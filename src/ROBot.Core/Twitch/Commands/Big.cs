using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Big : TwitchCommandHandler
    {
        public Big()
        {
            this.RequiresBroadcaster = true;
        }
        public override async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var targetPlayerName = cmd.Arguments?.Trim();
                    Player player = null;
                    if ((cmd.Sender.IsBroadcaster || cmd.Sender.IsModerator || cmd.Sender.IsGameAdmin || cmd.Sender.IsGameModerator) && !string.IsNullOrEmpty(targetPlayerName))
                    {
                        player = session.GetUserByName(targetPlayerName);
                    }
                    else
                    {
                        player = session.Get(cmd.Sender);
                    }

                    await connection.ScalePlayerAsync(player, 3f);
                }
            }
        }
    }
}
