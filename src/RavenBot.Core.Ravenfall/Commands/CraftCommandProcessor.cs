using System;
using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class CraftCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public CraftCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Send(cmd.Sender.Username,
                //broadcaster.Broadcast(
                Localization.GAME_NOT_STARTED);
                return;
            }

            var categoryAndType = cmd.Arguments?.Trim();
            var categories = Enum.GetNames(typeof(ItemCategory));
            var types = Enum.GetNames(typeof(ItemType));
            if (string.IsNullOrEmpty(categoryAndType))
            {
                //broadcaster.Broadcast(
                broadcaster.Send(cmd.Sender.Username,
                    $"You must specify an item category to craft. Currently supported item categories: {string.Join(", ", categories)}");
                return;
            }

            // !craft weapon
            // !craft amulet
            // !craft helm
            // !craft chest

            //var types = categoryAndType.Split(" ");

            if (categories.Any(x => x.Equals(categoryAndType, StringComparison.InvariantCultureIgnoreCase)) ||
                types.Any(x => x.Equals(categoryAndType, StringComparison.InvariantCultureIgnoreCase)))
            {
                var player = playerProvider.Get(cmd.Sender);
                if (Enum.TryParse(typeof(ItemType), categoryAndType, true, out var item))
                {
                    var category = ItemCategory.Armor;
                    switch ((ItemType)item)
                    {
                        case ItemType.Amulet:
                            category = ItemCategory.Amulet;
                            break;
                        case ItemType.Ring:
                            category = ItemCategory.Ring;
                            break;
                    }

                    await game.SendCraftAsync(player, category.ToString(), categoryAndType);
                    return;
                }
                else if (Enum.TryParse(typeof(ItemCategory), categoryAndType, true, out var weapon))
                {
                    await game.SendCraftAsync(player, categoryAndType, "");
                    return;
                }

            }

            //broadcaster.Broadcast(
            broadcaster.Send(cmd.Sender.Username,
            $"{categoryAndType} is not currently a craftable item category. Currently supported item categories: {string.Join(", ", categories)}");

        }

        public enum ItemCategory
        {
            Weapon,
            Armor,
            Ring,
            Amulet,
            Food,
            Potion
        }

        public enum ItemType
        {
            TwoHandedSword,
            OneHandedSword,
            TwoHandedAxe,
            OneHandedAxe,
            TwoHandedStaff,
            Bow,
            Mace,
            Helm,
            Chest,
            Gloves,
            Boots,
            Leggings,
            Shield,
            Shoulders,
            Ring,
            Amulet,
            Food,
            Potion
        }
    }
}