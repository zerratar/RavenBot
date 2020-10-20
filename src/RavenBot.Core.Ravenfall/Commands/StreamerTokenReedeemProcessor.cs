using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class StreamerTokenReedeemProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public StreamerTokenReedeemProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (!await game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                broadcaster.Broadcast(cmd.Sender.Username,
                    "You need to specify what to redeem, like: item, exp. See the options available in the Tavern.");
                return;
            }

            await game.RedeemStreamerTokenAsync(player, cmd.Arguments);
        }
    }
}