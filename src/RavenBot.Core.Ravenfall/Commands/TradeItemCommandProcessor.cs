using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class TradeItemCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public TradeItemCommandProcessor(
            IRavenfallClient game,
            IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;

        }
        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Send(cmd.Sender.Username,
                    Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);

            if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
            {
                broadcaster.Send(cmd.Sender.Username, cmd.Command + " <item> (optional: <amount>, default 1) <price per item>");
                return;
            }

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