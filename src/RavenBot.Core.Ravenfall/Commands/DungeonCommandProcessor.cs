using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class DungeonCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public DungeonCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                await this.game.JoinDungeonAsync(new EventJoinRequest(player, null));
                return;
            }
            else if (cmd.Arguments.Contains("stop", System.StringComparison.OrdinalIgnoreCase))
            {
                if (player.IsBroadcaster || player.IsModerator)
                {
                    await this.game.StopDungeonAsync(player);
                }
            }
            else if (cmd.Arguments.Contains("start", System.StringComparison.OrdinalIgnoreCase))
            {
                await this.game.DungeonStartAsync(player);
            }
        }
    }
}