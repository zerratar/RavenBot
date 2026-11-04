using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Monster : ChatCommandHandler
    {
        public override string Category => "Appearance";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("player", "Streamer and moderators only: the player to transform; leave it out to transform yourself.").NotRequired(),
        };
        public override string Description => "Turn your character into a monster for five minutes. The streamer and moderators can name another player to transform them instead.";
        public override string UsageExample => "!monster zerratar";

        public Monster()
        {
            RequiresBroadcaster = true;
        }
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

                    await connection[cmd].TurnIntoMonsterAsync(player);
                }
            }
        }
    }
}
