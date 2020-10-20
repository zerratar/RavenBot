using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class TravelCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        public TravelCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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

            var player = playerProvider.Get(cmd.Sender);
            var destination = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(destination))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.TRAVEL_NO_ARG);
                return;
            }

            await game.TravelAsync(player, destination);
        }
    }
}