using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Highest : ChatCommandHandler
    {
        public override string Description => "See which player in this stream has the highest level in a skill, the highest combat level, or the highest total level.";
        public override string UsageExample => "!highest fishing";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("skill", "A skill name such as fishing or attack, combat for the highest combat level, or all for the highest total level (the same as leaving it out).").NotRequired()
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
                    // a bare !highest is the highest total level; games before 0.9.7 threw on an empty skill
                    await connection[cmd].RequestHighestSkillAsync(player, string.IsNullOrWhiteSpace(cmd.Arguments) ? "all" : cmd.Arguments.Trim());
                }
            }
        }
    }
}
