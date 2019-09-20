using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class SailCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        public SailCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Send(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);

            if (cmd.Command.StartsWith("disembark"))
            {
                await game.DisembarkFerryAsync(player);
                return;
            }

            var destination = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(destination))
            {
                await game.EmbarkFerryAsync(player);
                return;
            }

            if (destination.StartsWith("stop"))
            {
                await game.DisembarkFerryAsync(player);
                return;
            }

            await game.TravelAsync(player, destination);
        }
    }
}