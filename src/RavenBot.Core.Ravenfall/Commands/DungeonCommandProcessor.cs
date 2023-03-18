using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class DungeonCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public DungeonCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                await this.game[cmd.CorrelationId].JoinDungeonAsync(player, null);
                return;
            }
            else if (cmd.Arguments.Contains("stop", System.StringComparison.OrdinalIgnoreCase))
            {
                if (player.IsBroadcaster || player.IsModerator)
                {
                    await this.game[cmd.CorrelationId].StopDungeonAsync(player);
                }
            }
            else if (cmd.Arguments.Contains("start", System.StringComparison.OrdinalIgnoreCase))
            {
                await this.game[cmd.CorrelationId].DungeonStartAsync(player);
            }
        }
    }
}