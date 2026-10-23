using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class MarketCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public MarketCommandProcessor(
            IRavenfallClient game,
            IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
            {
                await chat.SendReplyAsync(cmd, Localization.MARKET_TRADE_NO_ARG, cmd.Command);
                return;
            }

            var action = cmd.Arguments.Trim().Split(' ')[0].ToLower();
            if (action != "buy" && action != "sell" && action != "value")
            {
                await chat.SendReplyAsync(cmd, Localization.MARKET_TRADE_INVALID_ACTION, cmd.Command);
                return;
            }

            var player = playerProvider.Get(cmd);
            await this.game[cmd.CorrelationId].UseMarketAsync(player, cmd.Arguments);
        }
    }

    public class HelpCommandProcessor : Net.RavenfallCommandProcessor
    {
        public override Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            return chat.SendReplyAsync(cmd, "Please see all available game commands at https://www.ravenfall.stream/commands");
        }
    }
}
