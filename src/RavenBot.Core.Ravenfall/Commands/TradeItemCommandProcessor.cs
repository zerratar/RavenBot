using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class TradeItemCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public TradeItemCommandProcessor(
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
                await chat.SendReplyAsync(cmd, Localization.TRADE_NO_ARG, cmd.Command);
                return;
            }

            var player = playerProvider.Get(cmd);
            if (cmd.Command.Equals("sell", StringComparison.OrdinalIgnoreCase))
            {
                await this.game[cmd.CorrelationId].SellItemAsync(player, cmd.Arguments);
            }
            else if (cmd.Command.Equals("buy", StringComparison.OrdinalIgnoreCase))
            {

                await this.game[cmd.CorrelationId].BuyItemAsync(player, cmd.Arguments);
            }
        }
    }
}