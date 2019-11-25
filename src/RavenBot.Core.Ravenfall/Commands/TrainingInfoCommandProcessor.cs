using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class TrainingInfoCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public TrainingInfoCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }
        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Send(cmd.Sender.Username,
                //broadcaster.Broadcast(
                    Localization.GAME_NOT_STARTED);
                return;
            }


            var player = playerProvider.Get(cmd.Sender);
            if (player == null)
            {

                broadcaster.Send(cmd.Sender.Username, "Uh oh, bug when trying to leave :(");
            }

            await game.RequestTrainingInfoAsync(player);
        }
    }
}
