using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Highest : ChatCommandHandler
    {
        public override string Description => "This command allows for checking which player in the current game that has the highest skill";
        public override string UsageExample => "!highest fishing";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("skill", "Which skill to check").Required()
        };

        public override string Category => "Competition";
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
                    await connection[cmd].RequestHighestSkillAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
