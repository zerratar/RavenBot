using System.Threading.Tasks;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public interface IRavenfallClient
    {
        Task<bool> ProcessAsync(int serverPort);

        Task SendPlayerJoinAsync(Player player);
        Task SendPlayerTaskAsync(Player player, PlayerTask task, params string[] args);
        Task SendPlayerJoinArenaAsync(Player player);
        Task SendPlayerLeaveArenaAsync(Player player);

        Task SendStartArenaAsync(Player player);
        Task SendCancelArenaAsync(Player player);
        Task SendKickPlayerFromArenaAsync(Player player, Player targetPlayer);
        Task SendAddPlayerToArenaAsync(Player player, Player targetPlayer);

        Task SendKickPlayerAsync(Player targetPlayer);
        Task SendCraftAsync(Player targetPlayer, string itemCategory, string itemType);

        Task SendDuelRequestAsync(Player challenger, Player target);
        Task SendCancelDuelRequestAsync(Player player);
        Task SendAcceptDuelRequestAsync(Player player);
        Task SendDeclineDuelRequestAsync(Player player);

        Task SendPlayerJoinRaidAsync(Player player);
        Task SendRaidStartAsync(Player player);
        Task SendRequestPlayerResourcesAsync(Player player);

        Task SendRequestPlayerStatsAsync(Player player, string skill);
        Task SendRequestHighestSkillAsync(Player player, string skill);
        Task SendPlayerAppearanceUpdateAsync(Player player, string appearance);
        Task SendToggleHelmetAsync(Player player);
        Task SendTogglePetAsync(Player player);

        Task SendSellItemAsync(Player player, string itemQuery);
        Task SendBuyItemAsync(Player player, string itemQuery);
    }
}
