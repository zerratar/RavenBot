using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>The tower (the game's docs/tower-design.md): sends "tower_command" with what came after !tower; the game answers.</summary>
    public class Tower : ChatCommandHandler
    {
        public override string Category => "Events";
        public override string Description => "The tower: a climb of floors the whole stream makes together. On its own it takes you to the stream's floor, or to the town where everyone waits between floors (you keep training there); !tower leave takes you back to your training. Every floor has quests (defeat its monsters); then its boss can be challenged with !tower boss. After a clear everyone rests in the town until !tower climb takes them up. The streamer or a moderator decides at once, or players in the tower vote: !tower boss, !tower climb, !tower floor <number> (train on a floor already reached) and !tower close (everyone out for now). !tower info shows the floor, its quests and the boss.";
        public override string UsageExample => "!tower, !tower leave, !tower boss, !tower climb, !tower close, !tower floor 3, !tower info";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "leave, boss, climb, close, floor, town, rest, talk or info; leave it out to enter the tower.", "leave", "boss", "climb", "close", "floor", "town", "rest", "talk", "info").NotRequired(),
        };

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null) return;
            var connection = game.GetConnection(session);
            if (connection == null) return;
            var player = session.Get(cmd);
            if (player == null) return;
            await connection[cmd].TowerAsync(player, (cmd.Arguments ?? string.Empty).Trim());
        }
    }
}
