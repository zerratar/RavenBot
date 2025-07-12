using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
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
        public override string UsageExample => "!send 2 iron ore 10\n!send brute iron set";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "The target character (index or alias) to send the item to.").Required(),
            ChatCommandInput.Create("item", "The item to send.").Required(),
            ChatCommandInput.Create("amount", "The amount of the item to send.")
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
                    var targetPlayerName = cmd.Arguments?.Trim();
                    var query = cmd.Arguments?.Trim();


                    if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
                    {
                        await chat.SendReplyAsync(cmd, Localization.SEND_HELP, cmd.Command);
                        return;
                    }

                    await connection[cmd].SendItemAsync(player, query);
                }
            }
        }
    }
}
