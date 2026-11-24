using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>
    ///     The tower (the game's docs/tower-design.md; floors and parts: docs/tower-worlds-proposal.md section 12): sends
    ///     "tower_command" with what came after !tower; the game answers.
    /// </summary>
    public class Tower : ChatCommandHandler
    {
        public override string Category => "Events";
        public override string Description => "The tower: a climb the whole stream makes together. It has floors, each a world with its own town, and every floor has parts. On its own !tower takes you to the part the stream is on, or to the town where everyone waits between parts (you keep training there); !tower leave takes you back to your training. A part has objectives that count by themselves while the stream plays (its monsters defeated, materials gathered wherever you train, hidden things found); then its mini boss can be challenged with !tower boss, and the last part's floor boss opens the next floor. After a clear everyone rests in the town until !tower climb takes them up. The streamer or a moderator decides at once, or players in the tower vote: !tower boss, !tower climb, !tower part <number> (train on a part of this floor already reached; !tower 1-3 names the floor too, and !tower floor <number> goes to a floor's latest part) and !tower close (everyone out for now). !tower info shows the floor and part, its objectives and the boss.";
        public override string UsageExample => "!tower, !tower leave, !tower boss, !tower climb, !tower close, !tower part 3, !tower 1-3, !tower floor 1, !tower info";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "leave, boss, climb, close, part, floor, town, rest, talk or info, or a place such as 1-3; leave it out to enter the tower.", "leave", "boss", "climb", "close", "part", "floor", "town", "rest", "talk", "info").NotRequired(),
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
