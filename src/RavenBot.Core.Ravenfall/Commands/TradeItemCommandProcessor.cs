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
        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }


            if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.TRADE_NO_ARG, cmd.Command);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);
            if (cmd.Command.Equals("sell", StringComparison.OrdinalIgnoreCase))
            {
                await this.game.SellItemAsync(player, cmd.Arguments);
            }
            else if (cmd.Command.Equals("buy", StringComparison.OrdinalIgnoreCase))
            {

                await this.game.BuyItemAsync(player, cmd.Arguments);
            }
        }
    }
}