using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Leaderboard : ChatCommandHandler
    {
        public override string Description => "See your character's rank on the global leaderboard, overall or in one skill. The full list is at https://www.ravenfall.stream/leaderboard";
        public override string UsageExample => "!leaderboard fishing";
        public override string Category => "Competition";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("skill", "The skill to check, such as fishing; leave it out (or type all) for your overall rank.").NotRequired(),
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
                    var player = session.Get(cmd);
                    await connection[cmd].RequestHighscoreAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
