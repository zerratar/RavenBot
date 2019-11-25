using System.Threading.Tasks;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public interface IRavenfallClient
    {
        Task<bool> ProcessAsync(int serverPort);
        Task JoinAsync(Player player);
        Task SendPlayerTaskAsync(Player player, PlayerTask task, params string[] args);
        Task JoinArenaAsync(Player player);
        Task LeaveArenaAsync(Player player);
        Task LeaveAsync(Player player);
        Task StartArenaAsync(Player player);
        Task CancelArenaAsync(Player player);
        Task KickPlayerFromArenaAsync(Player player, Player targetPlayer);
        Task AddPlayerToArenaAsync(Player player, Player targetPlayer);
        Task KickAsync(Player targetPlayer);
        Task CraftAsync(Player targetPlayer, string itemCategory, string itemType);
        Task DuelRequestAsync(Player challenger, Player target);
        Task CancelDuelRequestAsync(Player player);
        Task PlayerCountAsync(Player player);
        Task AcceptDuelRequestAsync(Player player);
        Task DeclineDuelRequestAsync(Player player);
        Task JoinRaidAsync(Player player);
        Task RaidStartAsync(Player player);
        Task RequestIslandInfoAsync(Player player);
        Task RequestPlayerResourcesAsync(Player player);
        Task TravelAsync(Player player, string destination);
        Task RequestPlayerStatsAsync(Player player, string skill);
        Task RequestHighestSkillAsync(Player player, string skill);
        Task PlayerAppearanceUpdateAsync(Player player, string appearance);
        Task ItemDropEventAsync(Player player, string item);
        Task ObservePlayerAsync(Player player);
        Task ToggleHelmetAsync(Player player);
        Task TogglePetAsync(Player player);
        Task SellItemAsync(Player player, string itemQuery);
        Task BuyItemAsync(Player player, string itemQuery);
        Task VendorItemAsync(Player player, string itemQuery);
        Task GiftItemAsync(Player player, string itemQuery);
        Task ValueItemAsync(Player player, string itemQuery);
        Task DisembarkFerryAsync(Player player);
        Task EmbarkFerryAsync(Player player);
        Task RequestTrainingInfoAsync(Player player);
        Task RaidStreamerAsync(Player target, bool isRaidWar);
    }
}
