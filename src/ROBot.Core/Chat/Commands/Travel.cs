using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Travel : ChatCommandHandler
    {
        public override string Category => "Sailing";
        public override string Description => "Travel command is used for sailing between the different islands in game.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("destination", "Where do you want to sail?", "Home", "Away", "Ironhill", "Kyo", "Heim").Required()
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
                    var destination = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(destination))
                    {
                        await chat.SendReplyAsync(cmd, Localization.TRAVEL_NO_ARG);
                        return;
                    }

                    var player = session.Get(cmd);
                    if (player != null)
                        await connection[cmd].TravelAsync(player, destination);
                }
            }
        }
    }
}
