using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class TicTacToe : ChatCommandHandler
    {
        public override string Description => "Play Tic-Tac-Toe in the tavern while the streamer is there. !ttt starts a game, then chat places marks by square number.";
        public override string UsageExample => "!ttt 4";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("square", "A square from 1 to 9 to place your mark, or reset to start the board over. Leave empty to start a game.", "1", "2", "3", "4", "5", "6", "7", "8", "9", "reset").NotRequired()
        };

        public override string Category => "Tavern";
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
