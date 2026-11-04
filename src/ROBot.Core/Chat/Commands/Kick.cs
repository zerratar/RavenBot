using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Kick : ChatCommandHandler
    {
        public override bool RequiresBroadcaster => true;
        public override string Category => "Game";
        public override string Description => "Remove a player from the game, or everyone who is away or idle. Only the streamer and moderators can use it.";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "The player's name, or afk to kick everyone marked away plus anyone standing idle with nothing to do (players resting, raiding, in a dungeon, the arena, a duel or on the ferry stay).", "afk").Required(),
        };
        public override string UsageExample => "!kick zerratar";

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                    {
                        await chat.SendReplyAsync(cmd, Localization.KICK_PERM);
                        return;
                    }

                    var targetPlayerName = cmd.Arguments?.Trim();
                    if (string.IsNullOrEmpty(targetPlayerName))
                    {
                        await chat.SendReplyAsync(cmd, Localization.KICK_NO_USER);
                        return;
                    }

                    var sender = session.Get(cmd);
                    var target = session.GetUserByName(targetPlayerName);
                    await connection[cmd].KickAsync(sender, target);
                }
            }
        }
    }
}
