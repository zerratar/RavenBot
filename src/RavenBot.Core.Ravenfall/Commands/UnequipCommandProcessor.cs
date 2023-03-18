using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class UnequipCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public UnequipCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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


            var item = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(item))
            {

                chat.SendReply(cmd, "You have to use !unequip <item name> or !unequip all for unequipping all your items.");
                return;
            }

            await this.game[cmd.CorrelationId].UnequipAsync(player, item);
        }
    }
}