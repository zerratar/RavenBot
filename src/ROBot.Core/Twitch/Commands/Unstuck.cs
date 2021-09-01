using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Unstuck : TwitchCommandHandler
    {
        public Unstuck()
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
                    if (!string.IsNullOrEmpty(cmd.Arguments) && (cmd.Sender.IsBroadcaster || cmd.Sender.IsModerator || cmd.Sender.IsGameAdmin || cmd.Sender.IsGameModerator))
                    {
                        var player = session.GetUserByName(cmd.Arguments);
                        if (player != null)
                            await connection.UnstuckAsync(player);
                    }
                    else
                    {
                        var player = session.Get(cmd.Sender);
                        await connection.UnstuckAsync(player);
                    }
                }
            }
        }
    }
}
