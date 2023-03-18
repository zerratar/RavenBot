using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Join : ChatCommandHandler
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
                    var player = session.Join(cmd.Sender, cmd.Arguments);
                    await connection[cmd.CorrelationId].JoinAsync(player);
                }
            }
            else if (chat is Discord.DiscordCommandClient) // only for discord
            {
                chat.SendReply(cmd, "There are currently no active Ravenfall game sessions in this channel.");
            }
        }
    }
}
