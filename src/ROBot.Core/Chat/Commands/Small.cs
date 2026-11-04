
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Small : ChatCommandHandler
    {
        public override string Category => "Appearance";
        public Small()
        {
            RequiresBroadcaster = true;
        }
        public override string Description => "Shrinks your character to a tiny size for a short while. The streamer and moderators can shrink another player.";
        public override string UsageExample => "!small zerratar";

        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("player", "For the streamer and moderators: the player to shrink. Leave empty to shrink yourself.").NotRequired(),
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
                    var targetPlayerName = cmd.Arguments?.Trim();
                    User player = null;
                    if ((cmd.Sender.IsBroadcaster || cmd.Sender.IsModerator || cmd.Sender.IsGameAdmin || cmd.Sender.IsGameModerator) && !string.IsNullOrEmpty(targetPlayerName))
                    {
                        player = session.GetUserByName(targetPlayerName);
                    }
                    else
                    {
                        player = session.Get(cmd);
                    }

                    await connection[cmd].ScalePlayerAsync(player, 0.25f);
                }
            }
        }
    }
}
