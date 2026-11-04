using RavenBot.Core;
using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Leave : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "Leave the game with your current character. Leave first if you want to !join with another one. !leave ravebridge, !leave onsen, !leave arena and !leave ferry leave only that.";
        public override string UsageExample => "!leave";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("place", "Leave it out to leave the game. ravebridge, onsen, arena or ferry leaves only that and keeps you playing.", "ravebridge", "onsen", "arena", "ferry").NotRequired(),
        };

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null)
                return;

            // client might not accept a leave.
            //session.Leave(cmd.Sender.UserId);

            var connection = game.GetConnection(session);
            if (connection == null)
                return;

            var player = session.Get(cmd);
            if (player == null)
                return;

            // a word after it names what to leave: "!leave ravebridge" left the game altogether, the character gone from
            // the stream when only the island was meant (the user, 2026-11-04). A word it does not know leaves nothing
            // and says how, rather than guessing the whole game was meant.
            var place = (cmd.Arguments ?? string.Empty).Trim().ToLowerInvariant();
            var first = place.Split(' ')[0];
            switch (first)
            {
                case "":
                case "game":
                case "ravenfall":
                case "stream":
                case "now":
                    await connection[cmd].LeaveAsync(player);
                    return;

                case "ravebridge":
                case "rb":
                case "hero":
                case "island":
                case "event":
                case "waves":
                case "wave":
                    if (connection.ClientVersion != null && !GameVersion.IsLessThanOrEquals(connection.ClientVersion, "0.9.5.9"))
                        await connection[cmd].QuestAsync(player, "ravebridge leave");
                    return;

                case "onsen":
                case "rest":
                case "resting":
                case "spa":
                    await connection[cmd].LeaveOnsenAsync(player);
                    return;

                case "arena":
                    await connection[cmd].LeaveArenaAsync(player);
                    return;

                case "ferry":
                case "boat":
                case "ship":
                    await connection[cmd].DisembarkFerryAsync(player);
                    return;

                default:
                    await chat.SendReplyAsync(cmd, "!leave on its own leaves the game. To leave only Ravebridge, the onsen, the arena or the ferry: !leave ravebridge, !leave onsen, !leave arena or !leave ferry.");
                    return;
            }
        }
    }
}
