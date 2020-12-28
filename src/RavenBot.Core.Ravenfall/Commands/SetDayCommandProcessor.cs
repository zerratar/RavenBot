using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Twitch;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class SetDayCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public SetDayCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.RequiresBroadcaster = true;
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            var player = playerProvider.Get(cmd.Sender);
            await game.SetTimeOfDayAsync(player, 0, 15);
        }
    }
}
