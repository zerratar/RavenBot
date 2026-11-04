using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Stats : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Shows the levels of all your skills, or details about one skill, an item's stats or another player's levels.";
        public override string UsageExample => "!stats fishing";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "A skill (like fishing) for its level and exp to the next level, an item name for its stats, or a player's name for their levels. Leave empty for all your skills.")
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
                    await connection[cmd].RequestPlayerStatsAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
