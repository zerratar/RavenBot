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
        public override string Description => "The tower: a climb of floors the whole stream makes together. On its own it takes you to the stream's floor (and back to your training with !tower leave). Every floor has quests (defeat its monsters); when they are done its boss can be challenged with !tower boss: the streamer or a moderator starts it, or three players in the tower vote for it. !tower info shows the floor, its quests and the boss.";
        public override string UsageExample => "!tower, !tower leave, !tower boss, !tower info";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "leave, boss or info; leave it out to enter the tower.", "leave", "boss", "info").NotRequired(),
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
