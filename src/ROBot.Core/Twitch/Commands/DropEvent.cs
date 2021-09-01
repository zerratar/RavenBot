using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class DropEvent : TwitchCommandHandler
    {
        private readonly IUserRoleManager userRoleManager;

        public DropEvent(IUserRoleManager userRoleManager)
        {
            this.userRoleManager = userRoleManager;
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
                    var player = session.Get(cmd.Sender);
                    var item = cmd.Arguments?.Trim();

                    if (string.IsNullOrEmpty(item))
                    {
                        return;
                    }

                    if (!userRoleManager.IsAdministrator(player.UserId))
                    {
                        return;
                    }

                    await connection.ItemDropEventAsync(player, item);
                }
            }
        }
    }
}
