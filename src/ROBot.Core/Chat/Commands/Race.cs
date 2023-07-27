using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Race : ChatCommandHandler
    {
        public override string Category => "Tavern";
        public override string Description => "This is a Tavern Pet Race game, currently unavailable.";
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
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await connection[cmd].PlayPetRacingAsync(player);
                        return;
                    }

                    if (cmd.Arguments.Contains("reset", StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].ResetPetRacingAsync(player);
                        return;
                    }
                }
            }
        }
    }
}
