using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Race : TwitchCommandHandler
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
                        await connection.PlayPetRacingAsync(player);
                        return;
                    }

                    if (cmd.Arguments.Contains("reset", StringComparison.OrdinalIgnoreCase))
                    {
                        await connection.ResetPetRacingAsync(player);
                        return;
                    }
                }
            }
        }
    }
}
