using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class VillageCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public VillageCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);
            if (!string.IsNullOrEmpty(cmd.Arguments))
            {
                var arg = cmd.Arguments.ToLower().Trim();
                if (arg.Equals("res") || arg.Equals("resources"))
                {
                    await this.game[cmd.CorrelationId].RequestTownResourcesAsync(player);
                    return;
                }

                await this.game[cmd.CorrelationId].SetAllVillageHutsAsync(player, cmd.Arguments);
                return;
            }

            await this.game[cmd.CorrelationId].GetVillageBoostAsync(player);
        }
    }
}