using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Toggle : TwitchCommandHandler
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
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        twitch.Broadcast(channel, cmd.Sender.Username, Localization.VALUE_NO_ARG, cmd.Command);
                        return;
                    }

                    var player = session.Get(cmd.Sender);
                    if (cmd.Arguments.Contains("helm", StringComparison.OrdinalIgnoreCase))
                    {
                        await connection.ToggleHelmetAsync(player);
                    }
                    else if (cmd.Arguments.Contains("pet", StringComparison.OrdinalIgnoreCase))
                    {
                        await connection.TogglePetAsync(player);
                    }
                    else
                    {
                        twitch.Broadcast(channel, cmd.Sender.Username, Localization.TOGGLE_INVALID, cmd.Arguments);
                    }
                }
            }
        }
    }
}
