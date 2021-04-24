using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class GiantPlayerCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public GiantPlayerCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            //this.RequiresBroadcaster = true;
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            var sender = playerProvider.Get(cmd.Sender);
            if (!sender.IsBroadcaster)
            {
                return;
            }

            var targetPlayerName = cmd.Arguments?.Trim();
            Models.Player player = null;
            if (!string.IsNullOrEmpty(targetPlayerName))
            {
                player = playerProvider.Get(targetPlayerName);
            }
            else
            {
                player = playerProvider.Get(cmd.Sender);
            }

            await game.ScalePlayerAsync(player, 3f);
        }
    }
}
