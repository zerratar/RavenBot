using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Island : ChatCommandHandler
    {

        public override string Category => "Game";
        public override string Description => "See which island your character, or another player's, is on right now, or where the ferry is taking them.";
        public override string UsageExample => "!island zerratar";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("player", "The name of a player in this stream; leave it out to check yourself.").NotRequired(),
        };
        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    User player = null;
                    if (!string.IsNullOrEmpty(cmd.Arguments))
                    {
                        var username = cmd.Arguments;
                        if (cmd.Arguments.StartsWith("@"))
                        {
                            username = username.Substring(1);
                        }
                        player = session.GetUserByName(username);
                    }

                    if (player == null)
                    {
                        player = session.Get(cmd);
                    }

                    if (player != null)
                    {
                        await connection[cmd].RequestIslandInfoAsync(player);
                    }
                }
            }
        }
    }
}
