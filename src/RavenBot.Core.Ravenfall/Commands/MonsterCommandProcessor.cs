using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class MonsterCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public MonsterCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.RequiresBroadcaster = true;
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            var sender = playerProvider.Get(cmd.Sender);

            var targetPlayerName = cmd.Arguments?.Trim();
            Models.User player = null;
            if (!string.IsNullOrEmpty(targetPlayerName) && (sender.IsBroadcaster || sender.IsModerator))
            {
                player = playerProvider.Get(targetPlayerName);
            }
            else
            {
                player = playerProvider.Get(cmd.Sender);
            }

            await game.TurnIntoMonsterAsync(player);
        }
    }
}
