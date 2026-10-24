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

                    await connection[cmd].QuestInfoAsync(player);
                }
            }
        }
    }
}
