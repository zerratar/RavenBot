using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Sail : ChatCommandHandler
    {
        public override string Category => "Sailing";
        public override string Description => "Board the ferry to train sailing, or sail to another island. !sail stop gets you off the ferry.";
        public override string UsageExample => "!sail kyo";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("destination",
                "The island to sail to, or stop to leave the ferry. Leave empty to board the ferry and train sailing. " +
                "The streamer and moderators can add a player's name to send that player.",
                "Home", "Away", "Ironhill", "Kyo", "Heim", "Atria", "Eldara", "Mangrove", "stop").NotRequired()
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

                    var destination = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(destination))
                    {
                        await connection[cmd].EmbarkFerryAsync(player);
                        return;
                    }

                    if (destination.StartsWith("stop"))
                    {
                        await connection[cmd].DisembarkFerryAsync(player);
                        return;
                    }

                    await connection[cmd].TravelAsync(player, destination);
                }
            }
        }
    }
}
