using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class TicTacToe : ChatCommandHandler
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
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await connection[cmd].ActivateTicTacToeAsync(player);
                        return;
                    }

                    if (cmd.Arguments.Trim().Equals("reset", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].ResetTicTacToeAsync(player);
                        return;
                    }

                    if (int.TryParse(cmd.Arguments.Trim(), out var num))
                    {
                        await connection[cmd].PlayTicTacToeAsync(player, num);
                    }
                }
            }
        }
    }
}
