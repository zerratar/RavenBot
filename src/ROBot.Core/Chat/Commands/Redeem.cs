using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Redeem : ChatCommandHandler
    {
        public override string Description => "Redeem items using seasonal Tokens like Halloween Tokens and Christmas Tokens";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "What item you want to redeem").Required(),
            ChatCommandInput.Create("amount", "How many of the said item you want to redeem")
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
                    var player = session.Get(cmd);
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        chat.SendReply(cmd, Localization.REDEEM_NO_ARG);
                        return;
                    }

                    await connection[cmd].RedeemStreamerTokenAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
