using RavenBot.Core.Ravenfall.Models;
using ROBot.Core;
using ROBot.Core.GameServer;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ROBot.Tests
{
    public class MockRavenfallConnection : IRavenfallConnection
    {
        public Guid InstanceId => Guid.NewGuid();

        public IGameSession Session { get; set; }

        public IPEndPoint EndPoint => null;

        public string EndPointString => "127.0.0.1:1";

        public event EventHandler<GameSessionInfo> OnSessionInfoReceived;

        public Task AcceptDuelRequestAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task ActivateTicTacToeAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task AddPlayerToArenaAsync(Player player, Player targetPlayer)
        {
            return Task.CompletedTask;
        }

        public Task BuyItemAsync(Player player, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task CancelArenaAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task CancelDuelRequestAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public void Close()
        {
        }

        public Task CraftAsync(Player targetPlayer, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task CraftRequirementAsync(Player player, string itemName)
        {
            return Task.CompletedTask;
        }

        public Task DeclineDuelRequestAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task DisembarkFerryAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public Task DuelRequestAsync(Player challenger, Player target)
        {
            return Task.CompletedTask;
        }

        public Task DungeonStartAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task EmbarkFerryAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task EnchantAsync(Player player, string item)
        {
            return Task.CompletedTask;
        }

        public Task EquipAsync(Player player, string item)
        {
            return Task.CompletedTask;
        }

        public Task GetClientVersionAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task GetMaxMultiplierAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task GetPetAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task GetRestedStatusAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task GetScrollCountAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task GetStreamerTokenCountAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task GetVillageBoostAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task GiftItemAsync(Player player, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task InspectPlayerAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task ItemDropEventAsync(Player player, string item)
        {
            return Task.CompletedTask;
        }

        public Task JoinArenaAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task JoinAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task JoinDungeonAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task JoinOnsenAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task JoinRaidAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task KickAsync(Player targetPlayer)
        {
            return Task.CompletedTask;
        }

        public Task KickPlayerFromArenaAsync(Player player, Player targetPlayer)
        {
            return Task.CompletedTask;
        }

        public Task LeaveArenaAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task LeaveAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task LeaveOnsenAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task ObservePlayerAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task Ping(int correlationId)
        {
            return Task.CompletedTask;
        }

        public Task PlayerAppearanceUpdateAsync(Player player, string appearance)
        {
            return Task.CompletedTask;
        }

        public Task PlayerCountAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task PlayPetRacingAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task PlayTicTacToeAsync(Player player, int num)
        {
            return Task.CompletedTask;
        }

        public async Task<bool> ProcessAsync(int serverPort)
        {
            return true;
        }

        public Task RaidStartAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task RaidStreamerAsync(Player target, bool isRaidWar)
        {
            return Task.CompletedTask;
        }

        public Task RedeemStreamerTokenAsync(Player player, string query)
        {
            return Task.CompletedTask;
        }

        public Task ReloadGameAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task RequestHighestSkillAsync(Player player, string skill)
        {
            return Task.CompletedTask;
        }

        public Task RequestHighscoreAsync(Player player, string skill)
        {
            return Task.CompletedTask;
        }

        public Task RequestIslandInfoAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task RequestPlayerResourcesAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task RequestPlayerStatsAsync(Player player, string skill)
        {
            return Task.CompletedTask;
        }

        public Task RequestTrainingInfoAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task ResetPetRacingAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task ResetTicTacToeAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task RestartGameAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task ScalePlayerAsync(Player player, float v)
        {
            return Task.CompletedTask;
        }

        public Task SellItemAsync(Player player, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task SendPlayerTaskAsync(Player player, PlayerTask task, params string[] args)
        {
            return Task.CompletedTask;
        }

        public Task SetAllVillageHutsAsync(Player player, string skill)
        {
            return Task.CompletedTask;
        }

        public Task SetExpMultiplierAsync(Player player, int number)
        {
            return Task.CompletedTask;
        }

        public Task SetExpMultiplierLimitAsync(Player player, int number)
        {
            return Task.CompletedTask;
        }

        public Task SetPetAsync(Player player, string pet)
        {
            return Task.CompletedTask;
        }

        public Task SetTimeOfDayAsync(Player player, int totalTime, int freezeTime)
        {
            return Task.CompletedTask;
        }

        public Task StartArenaAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task StopDungeonAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task StopRaidAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task ToggleDiaperModeAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task ToggleHelmetAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task ToggleItemRequirementsAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task TogglePetAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task TravelAsync(Player player, string destination)
        {
            return Task.CompletedTask;
        }

        public Task TurnIntoMonsterAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task UnequipAsync(Player player, string item)
        {
            return Task.CompletedTask;
        }

        public Task UnstuckAsync(Player player)
        {
            return Task.CompletedTask;
        }

        public Task UseExpMultiplierScrollAsync(Player player, int number)
        {
            return Task.CompletedTask;
        }

        public Task ValueItemAsync(Player player, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task VendorItemAsync(Player player, string itemQuery)
        {
            return Task.CompletedTask;
        }
    }

}