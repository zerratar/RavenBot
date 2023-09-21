using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class EquipCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public EquipCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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


            var item = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(item))
            {

                await chat.SendReplyAsync(cmd, "You have to use !equip <item name> or !equip all for equipping your best items.");
                return;
            }

            await this.game[cmd.CorrelationId].EquipAsync(player, item);
        }
    }
}