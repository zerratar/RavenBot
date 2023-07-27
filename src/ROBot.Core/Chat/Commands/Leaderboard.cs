using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Leaderboard : ChatCommandHandler
    {
        public override string Description => "Check how you are faring on the leaderboard. See https://www.ravenfall.stream/leaderboard";
        public override string UsageExample => "!leaderboard fishing";
        public override string Category => "Competition";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("skill", "Which skill do you want to check? Leave empty for overall"),
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
