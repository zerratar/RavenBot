using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Weather : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "Tells the weather in the game. The broadcaster and moderators can change it: clear, fair, cloudy, overcast, rain, heavy rain or storm.";
        public override string UsageExample => "!weather storm";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("weather", "The weather to bring in; leave empty to hear what it is now.",
                "clear", "fair", "cloudy", "overcast", "rain", "heavy rain", "storm").NotRequired(),
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
                    // anyone may ask; the game itself checks who may change it
                    var player = session.Get(cmd);
                    if (player != null)
                        await connection[cmd].SetWeatherAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
