using RavenBot.Core;
using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Quest : ChatCommandHandler
    {
        public override string Category => "Tavern";
        public override string Description => "Ask the quest giver in the tavern for a quest. Quests are coming in a future update.";
        public override string UsageExample => "!quest";
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
                    if (player == null)
                        return;

                    // games from 0.9.6 have quests to take and give up (!quest accept 2); older ones only the teaser
                    if (connection.ClientVersion != null && !GameVersion.IsLessThanOrEquals(connection.ClientVersion, "0.9.5.9"))
                        await connection[cmd].QuestAsync(player, cmd.Arguments ?? string.Empty);
                    else
                        await connection[cmd].QuestInfoAsync(player);
                }
            }
        }
    }
}
