using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Send : ChatCommandHandler
    {
        // command for sending a target item to another character that the player owns
        // !send <target> <item> <amount>
        public override string Category => "Game";
        public override string Description => "Send an item to one of your other characters.";
        public override string UsageExample => "!send zerratar iron ore 10";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "The target player to send the item to.").Required(),
            ChatCommandInput.Create("item", "The item to send.").Required(),
            ChatCommandInput.Create("amount", "The amount of the item to send.")
        };

        public override Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var player = session.Get(cmd);
                    var targetPlayerName = cmd.Arguments?.Trim();
                    var query = cmd.Arguments?.Trim();
                    connection[cmd].SendItemAsync(player, query);
                }
            }
            return Task.CompletedTask;
        }
    }
}
