using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class TinyPlayerCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public TinyPlayerCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.RequiresBroadcaster = true;
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            var sender = playerProvider.Get(cmd);

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

            await this.game[cmd.CorrelationId].ScalePlayerAsync(player, 0.25f);
        }
    }
}
