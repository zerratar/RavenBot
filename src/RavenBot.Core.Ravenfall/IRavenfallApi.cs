using System.Threading.Tasks;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public interface IRavenfallApi
    {
        Task JoinAsync(User author);
        Task ToggleItemRequirementsAsync(User author);
        Task ToggleDiaperModeAsync(User author);
        Task SetExpMultiplierAsync(User author, int number);
        Task UseExpMultiplierScrollAsync(User author, int number);
        Task SetExpMultiplierLimitAsync(User author, int number);
        Task SendPlayerTaskAsync(User author, PlayerTask task, params string[] args);
        Task JoinArenaAsync(User author);
        Task LeaveArenaAsync(User author);
        Task LeaveAsync(User author);
        Task StartArenaAsync(User author);
        Task CancelArenaAsync(User author);
        Task TurnIntoMonsterAsync(User author);
        Task RestartGameAsync(User author);
        Task KickPlayerFromArenaAsync(User author, User targetPlayer);
        Task UnstuckAsync(User author, string args);
        Task InspectPlayerAsync(User author);
        Task AddPlayerToArenaAsync(User author, User targetPlayer);
        Task KickAsync(User author, User targetPlayer);
        Task CraftAsync(User author, string itemQuery);
        Task CookAsync(User author, string itemQuery);

        Task MineAsync(User author, string itemQuery);
        Task ChopAsync(User author, string itemQuery);
        Task FarmAsync(User author, string itemQuery);
        Task FishAsync(User author, string itemQuery);
        Task GatherAsync(User author, string itemQuery);
        Task BrewAsync(User author, string itemQuery);

        Task RequestHighscoreAsync(User author, string skill);
        Task DuelRequestAsync(User author, User target);
        Task ReloadGameAsync(User author);
        Task CancelDuelRequestAsync(User author);
        Task PlayerCountAsync(User author);
        Task SetTimeOfDayAsync(User author, int totalTime, int freezeTime);
        Task AcceptDuelRequestAsync(User author);
        Task RedeemStreamerTokenAsync(User author, string query);
        Task GetStreamerTokenCountAsync(User author);
        Task GetScrollCountAsync(User author);
        Task DeclineDuelRequestAsync(User author);
        Task UnequipAsync(User author, string item);
        Task EquipAsync(User author, string item);
        Task EnchantAsync(User author, string item);
        Task DisenchantAsync(User author, string item);
        Task ClearEnchantmentCooldownAsync(User author);
        Task GetEnchantmentCooldownAsync(User author);
        Task SetPetAsync(User author, string pet);
        Task GetPetAsync(User author);
        Task GetMaxMultiplierAsync(User author);
        Task GetVillageBoostAsync(User author);
        Task SetAllVillageHutsAsync(User author, string skill);
        Task GetVillageStatsAsync(User author);
        Task JoinRaidAsync(User author, string code);
        Task AutoJoinRaidAsync(User player, string query);
        Task StopRaidAsync(User author);
        Task RaidStartAsync(User author);
        Task DungeonStartAsync(User author);
        Task JoinDungeonAsync(User author, string code);
        Task AutoJoinDungeonAsync(User player, string query);
        Task StopDungeonAsync(User author);
        Task CraftRequirementAsync(User author, string itemName);
        Task CountItemAsync(User author, string itemName);
        Task ExamineItemAsync(User author, string itemName);
        Task RequestIslandInfoAsync(User author);
        Task RequestPlayerResourcesAsync(User author);
        Task RequestTownResourcesAsync(User author);
        Task TravelAsync(User author, string destination);
        Task RequestPlayerStatsAsync(User author, string skill);
        Task RequestPlayerEquipmentStatsAsync(User author, string target);
        Task RequestHighestSkillAsync(User author, string skill);
        Task PlayerAppearanceUpdateAsync(User author, string appearance);
        Task ItemDropEventAsync(User author, string item);
        Task ObservePlayerAsync(User author);
        Task ToggleHelmetAsync(User author);
        Task TogglePetAsync(User author);

        Task UseMarketAsync(User author, string itemQuery);
        Task UseVendorAsync(User author, string itemQuery);

        Task SellItemAsync(User author, string itemQuery);
        Task BuyItemAsync(User author, string itemQuery);
        Task VendorItemAsync(User author, string itemQuery);
        Task GiftItemAsync(User author, string itemQuery);
        Task ValueItemAsync(User author, string itemQuery);
        Task DisembarkFerryAsync(User author);
        Task EmbarkFerryAsync(User author);
        Task RequestTrainingInfoAsync(User author);
        Task RaidStreamerAsync(User author, User target, bool isRaidWar);
        Task GetClientVersionAsync(User author);

        // ONSEN
        Task LeaveOnsenAsync(User author);
        Task JoinOnsenAsync(User author);
        Task GetRestedStatusAsync(User author);


        // TAVERN GAMES
        Task PlayTicTacToeAsync(User author, int num);
        Task ScalePlayerAsync(User author, float v);
        Task ActivateTicTacToeAsync(User author);
        Task ResetTicTacToeAsync(User author);
        Task ResetPetRacingAsync(User author);
        Task PlayPetRacingAsync(User author);

        Task UpdateGameAsync(User player);

        // KEEP ALIVE
        Task Ping(int correlationId);


        // CLAN
        /// <summary>
        /// allow players to join clans that does not require invites.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="arguments">can be a required join code</param>
        /// <returns></returns>
        Task JoinClanAsync(User author, string arguments);
        Task LeaveClanAsync(User author, string argument);
        Task RemoveFromClanAsync(User author, User targetPlayer);
        Task SendClanInviteAsync(User author, User targetPlayer);
        Task AcceptClanInviteAsync(User author, string argument);
        Task DeclineClanInviteAsync(User author, string argument);
        Task PromoteClanMemberAsync(User author, User targetPlayer, string argument);
        Task DemoteClanMemberAsync(User author, User targetPlayer, string argument);
        /// <summary>
        /// clan info, displays the current clan and clan level
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        Task GetClanInfoAsync(User author, string argument);

        /// <summary>
        /// gets some statistics for the clan
        /// how many members, clan skill levels
        /// how many members of each type
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        Task GetClanStatsAsync(User author, string argument);
        Task UseItemAsync(User player, string arguments);
        Task TeleportAsync(User player, string island);
        Task GetStatusEffectsAsync(User player, string arguments);
        Task GetItemUsageAsync(User player, string arguments);
        Task SendChatMessageAsync(User player, string message);
        Task AutoRestAsync(User player, int startRestTime, int endRestTime);
        Task StopAutoRestAsync(User player);
        Task RequestAutoRestStatusAsync(User player);
        Task AutoUseAsync(User player, int amount);
        Task StopAutoUseAsync(User player);
        Task RequestAutoUseStatusAsync(User player);
        Task SendChannelStateAsync(string platform, string channelName, bool inChannel, string message);
        Task GetDpsAsync(User player);
        Task SetRaidCombatStyleAsync(User player, string targetSkill);
        Task SetDungeonCombatStyleAsync(User player, string targetSkill);
        Task ClearDungeonCombatStyleAsync(User player);
        Task ClearRaidCombatStyleAsync(User player);
    }
}
