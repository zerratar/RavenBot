using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    /// <summary>!weather: tells the weather in the game, or brings another (the game checks who may change it).</summary>
    public class WeatherCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public WeatherCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            var player = playerProvider.Get(cmd);
            await this.game[cmd.CorrelationId].SetWeatherAsync(player, cmd.Arguments?.Trim());
        }
    }
}
