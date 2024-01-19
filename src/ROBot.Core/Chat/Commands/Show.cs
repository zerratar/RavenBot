using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Show : ChatCommandHandler
    {
        public Show()
        {
            RequiresBroadcaster = true;
        }
        public override string Category => "Game";
        public override string Description => "Allows for focusing the camera on your character.";
        public override string UsageExample => "!show";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "If you're a broadcaster or moderator you can observe a target player.")
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
                    if (!cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator && !cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsSubscriber)
                    {
                        await chat.SendReplyAsync(cmd, Localization.OBSERVE_PERM);
                        return;
                    }

                    var targetPlayerName = cmd.Arguments?.Trim();
                    var player = string.IsNullOrEmpty(targetPlayerName)
                        ? session.Get(cmd.Sender)
                        : session.GetUserByName(targetPlayerName);

                    await connection[cmd].ObservePlayerAsync(player);
                }
            }
        }
    }
}
