using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Redeem : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Redeem items with seasonal tokens (Halloween, Christmas), or buy from the streamer's tavern shelves with coins (the coins go to the village)";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The item, or the number of a tavern shelf").Required(),
            ChatCommandInput.Create("amount", "How many to buy from a tavern shelf (1 when left out)")
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
                        await chat.SendReplyAsync(cmd, Localization.REDEEM_NO_ARG);
                        return;
                    }

                    await connection[cmd].RedeemStreamerTokenAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
