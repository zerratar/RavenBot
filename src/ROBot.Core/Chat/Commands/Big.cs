using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Big : ChatCommandHandler
    {
        public override string Description => "Turn a character into a giant, three times their size. Anyone can grow themselves; the streamer and moderators can also name another player.";
        public override string UsageExample => "!big zerratar";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "The player to grow (streamer and moderators only); leave it out to grow yourself.").NotRequired(),
        };
        public override string Category => "Appearance";
        public Big()
        {
            RequiresBroadcaster = true;
        }
        public override async Task HandleAsync(IBotServer game, IChatCommandClient twitch, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var targetPlayerName = cmd.Arguments?.Trim();
                    User player = null;
                    if ((cmd.Sender.IsBroadcaster || cmd.Sender.IsModerator || cmd.Sender.IsGameAdmin
                        || cmd.Sender.IsGameModerator) && !string.IsNullOrEmpty(targetPlayerName))
                    {
                        player = session.GetUserByName(targetPlayerName);
                    }
                    else
                    {
                        player = session.Get(cmd);
                    }

                    await connection[cmd].ScalePlayerAsync(player, 3f);
                }
            }
        }
    }
}
