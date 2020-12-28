using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class PetCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        public PetCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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


            var pet = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(pet))
            {
                await game.GetPetAsync(player);
                return;
            }

            await game.SetPetAsync(player, pet);
        }
    }

    public class EquipCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        public EquipCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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


            var item = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(item))
            {

                broadcaster.Broadcast(cmd.Sender.Username, "You have to use !equip <item name> or !equip all for equipping your best items.");
                return;
            }

            await game.EquipAsync(player, item);
        }
    }

    public class UnequipCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        public UnequipCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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


            var item = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(item))
            {

                broadcaster.Broadcast(cmd.Sender.Username, "You have to use !unequip <item name> or !unequip all for unequipping all your items.");
                return;
            }

            await game.UnequipAsync(player, item);
        }
    }
}