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
        public override string Description => "Kick target player or afk players, this command can only be used by the broadcaster or a moderator.";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "Who do you want to kick?", "<player name>", "afk").Required(),
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
