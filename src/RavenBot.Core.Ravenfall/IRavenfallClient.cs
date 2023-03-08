using System.Threading.Tasks;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public interface IRavenfallClient
    {
        Task<bool> ProcessAsync(int serverPort);
        Task JoinAsync(User player);
        Task ToggleItemRequirementsAsync(User player);
        Task ToggleDiaperModeAsync(User player);
        Task SetExpMultiplierAsync(User player, int number);
        Task UseExpMultiplierScrollAsync(User player, int number);
        Task SetExpMultiplierLimitAsync(User player, int number);
        Task SendPlayerTaskAsync(User player, PlayerTask task, params string[] args);
        Task JoinArenaAsync(User player);
        Task LeaveArenaAsync(User player);
        Task LeaveAsync(User player);
        Task StartArenaAsync(User player);
        Task CancelArenaAsync(User player);
        Task TurnIntoMonsterAsync(User player);
        Task RestartGameAsync(User player);
        Task KickPlayerFromArenaAsync(User player, User targetPlayer);
        Task UnstuckAsync(User player);
        Task InspectPlayerAsync(User player);
        Task AddPlayerToArenaAsync(User player, User targetPlayer);
        Task KickAsync(User targetPlayer);
        Task CraftAsync(User targetPlayer, string itemQuery);
        Task RequestHighscoreAsync(User player, string skill);
        Task DuelRequestAsync(User challenger, User target);
        Task ReloadGameAsync(User player);
        Task CancelDuelRequestAsync(User player);
        Task PlayerCountAsync(User player);
        Task SetTimeOfDayAsync(User player, int totalTime, int freezeTime);
        Task AcceptDuelRequestAsync(User player);
        Task RedeemStreamerTokenAsync(User player, string query);
        Task GetStreamerTokenCountAsync(User player);
        Task GetScrollCountAsync(User player);
        Task DeclineDuelRequestAsync(User player);
        Task UnequipAsync(User player, string item);
        Task EquipAsync(User player, string item);
        Task EnchantAsync(User player, string item);
        Task DisenchantAsync(User player, string item);
        Task SetPetAsync(User player, string pet);
        Task GetPetAsync(User player);
        Task GetMaxMultiplierAsync(User player);
        Task GetVillageBoostAsync(User player);
        Task SetAllVillageHutsAsync(User player, string skill);
        Task JoinRaidAsync(EventJoinRequest player);
        Task StopRaidAsync(User player);
        Task RaidStartAsync(User player);
        Task DungeonStartAsync(User player);
        Task JoinDungeonAsync(EventJoinRequest player);
        Task StopDungeonAsync(User player);
        Task CraftRequirementAsync(User player, string itemName);
        Task CountItemAsync(User player, string itemName);
        Task RequestIslandInfoAsync(User player);
        Task RequestPlayerResourcesAsync(User player);
        Task TravelAsync(User player, string destination);
        Task RequestPlayerStatsAsync(User player, string skill);
        Task RequestHighestSkillAsync(User player, string skill);
        Task PlayerAppearanceUpdateAsync(User player, string appearance);
        Task ItemDropEventAsync(User player, string item);
        Task ObservePlayerAsync(User player);
        Task ToggleHelmetAsync(User player);
        Task TogglePetAsync(User player);
        Task SellItemAsync(User player, string itemQuery);
        Task BuyItemAsync(User player, string itemQuery);
        Task VendorItemAsync(User player, string itemQuery);
        Task GiftItemAsync(User player, string itemQuery);
        Task ValueItemAsync(User player, string itemQuery);
        Task DisembarkFerryAsync(User player);
        Task EmbarkFerryAsync(User player);
        Task RequestTrainingInfoAsync(User player);
        Task RaidStreamerAsync(User target, bool isRaidWar);
        Task GetClientVersionAsync(User player);

        // ONSEN
        Task LeaveOnsenAsync(User player);
        Task JoinOnsenAsync(User player);
        Task GetRestedStatusAsync(User player);


        // TAVERN GAMES
        Task PlayTicTacToeAsync(User player, int num);
        Task ScalePlayerAsync(User player, float v);
        Task ActivateTicTacToeAsync(User player);
        Task ResetTicTacToeAsync(User player);
        Task ResetPetRacingAsync(User player);
        Task PlayPetRacingAsync(User player);

        // KEEP ALIVE
        Task Ping(int correlationId);


        // CLAN
        /// <summary>
        /// allow players to join clans that does not require invites.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="arguments">can be a required join code</param>
        /// <returns></returns>
        Task JoinClanAsync(User player, string arguments);
        Task LeaveClanAsync(User player, string argument);
        Task RemoveFromClanAsync(User player, User targetPlayer);
        Task SendClanInviteAsync(User player, User targetPlayer);
        Task AcceptClanInviteAsync(User player, string argument);
        Task DeclineClanInviteAsync(User player, string argument);
        Task PromoteClanMemberAsync(User player, User targetPlayer, string argument);
        Task DemoteClanMemberAsync(User player, User targetPlayer, string argument);
        /// <summary>
        /// clan info, displays the current clan and clan level
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        Task GetClanInfoAsync(User player, string argument);

        /// <summary>
        /// gets some statistics for the clan
        /// how many members, clan skill levels
        /// how many members of each type
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        Task GetClanStatsAsync(User player, string argument);
    }
}
