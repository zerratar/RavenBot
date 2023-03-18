using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class SailCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public SailCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                chat.SendReply(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);

            if (cmd.Command.StartsWith("disembark"))
            {
                await this.game[cmd.CorrelationId].DisembarkFerryAsync(player);
                return;
            }

            var destination = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(destination))
            {
                await this.game[cmd.CorrelationId].EmbarkFerryAsync(player);
                return;
            }

            if (destination.StartsWith("stop"))
            {
                await this.game[cmd.CorrelationId].DisembarkFerryAsync(player);
                return;
            }

            await this.game[cmd.CorrelationId].TravelAsync(player, destination);
        }
    }
}