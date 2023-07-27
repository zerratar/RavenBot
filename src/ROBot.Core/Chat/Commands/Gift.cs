using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Gift : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Gift items to a target player. The order of item and amount does not matter as long as they are distinguishable of what is what. But player name must always be the first argument.";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("player", "The player that you want to gift an item to.").Required(),
            ChatCommandInput.Create("item", "The item that you want to give.").Required(),
            ChatCommandInput.Create("amount", "The amount of that item you want to give. Default is 1, you can use 'k', 'm', 't', 'b'"),
        };

        public override string UsageExample => "!gift zerratar 1k burned shrimps";
        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
                    {
                        chat.SendReply(cmd, Localization.GIFT_HELP, cmd.Command);
                        return;
                    }

                    var player = session.Get(cmd);
                    if (player != null)
                        await connection[cmd].GiftItemAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
