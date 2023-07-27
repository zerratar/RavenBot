using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Leave : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "Use this command to leave the game with your current character. This is required if you want to play with another character.";
        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null)
                return;

            // client might not accept a leave.
            //session.Leave(cmd.Sender.UserId);

            var connection = game.GetConnection(session);
            if (connection != null)
            {
                var player = session.Get(cmd);
                if (player != null)
                {
                    await connection[cmd].LeaveAsync(player);
                }
            }
        }
    }
}
