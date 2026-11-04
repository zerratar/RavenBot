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
        public override string Description => "Sail to another island on the ferry; you get off when it docks there.";
        public override string UsageExample => "!travel ironhill";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("destination",
                "The island to sail to. Mangrove (the banana island) only once the village has unlocked it. " +
                "The streamer and moderators can add a player's name to send that player.",
                "Home", "Away", "Ironhill", "Kyo", "Heim", "Atria", "Eldara", "Mangrove").Required()
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
