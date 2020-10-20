using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ForceAddPlayerCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ForceAddPlayerCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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

            if (!cmd.Sender.IsBroadcaster || !cmd.Sender.DisplayName.ToLower().Equals("zerratar"))
                return;

            if (string.IsNullOrEmpty(cmd.Arguments))
                return;

            var values = cmd.Arguments.Split(' ');
            if (values.Length <= 1)
                return;

            var player = playerProvider.Get(values[0], values[1]);
            await game.JoinAsync(player);
        }
    }
}