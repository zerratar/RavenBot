using System.Threading.Tasks;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public interface IRavenfallClient
    {
        Task<bool> ProcessAsync(int serverPort);
        Task JoinAsync(Player player);
        Task ToggleItemRequirementsAsync(Player player);
        Task ToggleDiaperModeAsync(Player player);
        Task SetExpMultiplierAsync(Player player, int number);
        Task UseExpMultiplierScrollAsync(Player player, int number);
        Task SetExpMultiplierLimitAsync(Player player, int number);
        Task SendPlayerTaskAsync(Player player, PlayerTask task, params string[] args);
        Task JoinArenaAsync(Player player);
        Task LeaveArenaAsync(Player player);
        Task LeaveAsync(Player player);
        Task StartArenaAsync(Player player);
        Task CancelArenaAsync(Player player);
        Task TurnIntoMonsterAsync(Player player);
        Task RestartGameAsync(Player player);
        Task KickPlayerFromArenaAsync(Player player, Player targetPlayer);
        Task UnstuckAsync(Player player);
        Task InspectPlayerAsync(Player player);
        Task AddPlayerToArenaAsync(Player player, Player targetPlayer);
        Task KickAsync(Player targetPlayer);
        Task CraftAsync(Player targetPlayer, string itemQuery);
        Task RequestHighscoreAsync(Player player, string skill);
        Task DuelRequestAsync(Player challenger, Player target);
        Task ReloadGameAsync(Player player);
        Task CancelDuelRequestAsync(Player player);
        Task PlayerCountAsync(Player player);
        Task SetTimeOfDayAsync(Player player, int totalTime, int freezeTime);
        Task AcceptDuelRequestAsync(Player player);
        Task RedeemStreamerTokenAsync(Player player, string query);
        Task GetStreamerTokenCountAsync(Player player);
        Task GetScrollCountAsync(Player player);
        Task DeclineDuelRequestAsync(Player player);
        Task UnequipAsync(Player player, string item);
        Task EquipAsync(Player player, string item);
        Task EnchantAsync(Player player, string item);
        Task SetPetAsync(Player player, string pet);
        Task GetPetAsync(Player player);
        Task GetMaxMultiplierAsync(Player player);
        Task GetVillageBoostAsync(Player player);
        Task SetAllVillageHutsAsync(Player player, string skill);
        Task JoinRaidAsync(EventJoinRequest player);
        Task StopRaidAsync(Player player);
        Task RaidStartAsync(Player player);
        Task DungeonStartAsync(Player player);
        Task JoinDungeonAsync(EventJoinRequest player);
        Task StopDungeonAsync(Player player);
        Task CraftRequirementAsync(Player player, string itemName);
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
        Task GetClientVersionAsync(Player player);

        // ONSEN
        Task LeaveOnsenAsync(Player player);
        Task JoinOnsenAsync(Player player);
        Task GetRestedStatusAsync(Player player);


        // TAVERN GAMES
        Task PlayTicTacToeAsync(Player player, int num);
        Task ScalePlayerAsync(Player player, float v);
        Task ActivateTicTacToeAsync(Player player);
        Task ResetTicTacToeAsync(Player player);
        Task ResetPetRacingAsync(Player player);
        Task PlayPetRacingAsync(Player player);

        // KEEP ALIVE
        Task Ping(int correlationId);


        // CLAN
        /// <summary>
        /// allow players to join clans that does not require invites.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="arguments">can be a required join code</param>
        /// <returns></returns>
        Task JoinClanAsync(Player player, string arguments);
        Task LeaveClanAsync(Player player);
        Task RemoveFromClanAsync(Player player, Player targetPlayer);
        Task SendClanInviteAsync(Player player, Player targetPlayer);
        Task AcceptClanInviteAsync(Player player, string argument);
        Task DeclineClanInviteAsync(Player player, string argument);
        Task PromoteClanMemberAsync(Player player, Player targetPlayer, string argument);
        Task DemoteClanMemberAsync(Player player, Player targetPlayer, string argument);
        /// <summary>
        /// clan info, displays the current clan and clan level
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        Task GetClanInfoAsync(Player player);

        /// <summary>
        /// gets some statistics for the clan
        /// how many members, clan skill levels
        /// how many members of each type
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        Task GetClanStatsAsync(Player player);
    }
}
