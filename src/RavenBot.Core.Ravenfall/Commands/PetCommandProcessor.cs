using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class PetCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public PetCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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


            var pet = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(pet))
            {
                await this.game[cmd.CorrelationId].GetPetAsync(player);
                return;
            }

            await this.game[cmd.CorrelationId].SetPetAsync(player, pet);
        }
    }
}