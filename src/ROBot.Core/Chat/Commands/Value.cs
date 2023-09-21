using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Value : ChatCommandHandler
    {
        public override string Description => "Gets the vendor value of the specified item";
        public override string UsageExample => "!value rune 2h sword";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "Which item you want to check how much its worth").Required()
        };

        public override string Category => "Items";
        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await chat.SendReplyAsync(cmd, Localization.VALUE_NO_ARG, cmd.Command);
                        return;
                    }

                    var player = session.Get(cmd);
                    if (player != null)
                        await connection[cmd].ValueItemAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
