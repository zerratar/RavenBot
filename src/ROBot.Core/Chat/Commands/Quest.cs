using RavenBot.Core;
using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Quest : ChatCommandHandler
    {
        public override string Category => "Tavern";
        public override string Description => "See and manage your quests. !quest alone lists the quests you have, today's offers and the weekly quest; take one with !quest accept 1, and on a quest chain step that says \"Bring 150 Logs\" hand the items in with !quest deliver.";
        public override string UsageExample => "!quest deliver";

        private static readonly IReadOnlyList<ChatCommandInput> QuestInputs = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action",
                "What to do; leave empty to list your quests (on a quest chain, its current step). " +
                "list shows every quest and today's offers even while you are on a chain. " +
                "accept (or take) takes a quest. " +
                "chains lists the quest chains and begin starts one. " +
                "deliver (or handin) hands in the items a chain step asks for, like \"Bring 150 Logs\", straight from your inventory. " +
                "answer (or say) answers a chain step's question. " +
                "abandon (or drop) gives up a quest. " +
                "village shows the streamer's village story, give (or donate) brings it the items it asks for and vote votes on its choices. " +
                "stream shows the stream quest everyone works on together. " +
                "title lists your titles or wears one, achievements shows your medals.",
                "list", "accept", "chains", "begin", "deliver", "answer", "abandon", "village", "give", "vote", "stream", "title", "achievements").NotRequired(),
            ChatCommandInput.Create("value",
                "accept: 1, 2 or 3 for one of today's offers, weekly for the weekly quest or seasonal for the event quest (a chain's name begins that chain). " +
                "begin: the chain's name or its number in !quest chains. " +
                "answer and vote: the number of your choice. " +
                "abandon: the quest's number in !quest, or chain to leave your quest chain. " +
                "title: the title's number, or off to wear none. " +
                "stream: start 1-3 or stop, for the streamer and moderators only. " +
                "deliver and give need nothing more.").NotRequired(),
        };

        /// <summary>
        /// Only !quest (and !quests) describes the quest inputs; the commands built on it (!title, !ravebridge,
        /// !hero, !achievements) send their own sub command and describe their own inputs, or none.
        /// </summary>
        public override IReadOnlyList<ChatCommandInput> Inputs => GetType() == typeof(Quest) || GetType() == typeof(Quests) ? QuestInputs : null;

        /// <summary>What the game is sent after quest_command: the chat's words, or a sub command first (!title, !achievements).</summary>
        protected virtual string ArgumentsFor(ICommand cmd) => cmd.Arguments ?? string.Empty;
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
                        await connection[cmd].QuestAsync(player, ArgumentsFor(cmd));
                    else
                        await connection[cmd].QuestInfoAsync(player);
                }
            }
        }
    }
}
