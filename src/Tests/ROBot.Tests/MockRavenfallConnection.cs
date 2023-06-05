using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
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

        public IRavenfallApi Api => throw new NotImplementedException();

        public IRavenfallApi this[ICommand cmd] => throw new NotImplementedException();

        public event EventHandler<GameSessionInfo> OnSessionInfoReceived;

        public Task AcceptClanInviteAsync(User player, string argument)
        {
            return Task.CompletedTask;
        }

        public Task AcceptDuelRequestAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task ActivateTicTacToeAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task AddPlayerToArenaAsync(User player, User targetPlayer)
        {
            return Task.CompletedTask;
        }

        public Task BuyItemAsync(User player, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task CancelArenaAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task CancelDuelRequestAsync(User player)
        {
            return Task.CompletedTask;
        }

        public void Close()
        {
        }

        public Task CountItemAsync(User player, string itemName)
        {
            throw new NotImplementedException();
        }

        public Task CraftAsync(User targetPlayer, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task CraftRequirementAsync(User player, string itemName)
        {
            return Task.CompletedTask;
        }

        public Task DeclineClanInviteAsync(User player, string argument)
        {
            throw new NotImplementedException();
        }

        public Task DeclineDuelRequestAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task DemoteClanMemberAsync(User player, User targetPlayer, string argument)
        {
            throw new NotImplementedException();
        }

        public Task DisembarkFerryAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task DisenchantAsync(User player, string item)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public Task DuelRequestAsync(User challenger, User target)
        {
            return Task.CompletedTask;
        }

        public Task DungeonStartAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task EmbarkFerryAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task EnchantAsync(User player, string item)
        {
            return Task.CompletedTask;
        }

        public Task EquipAsync(User player, string item)
        {
            return Task.CompletedTask;
        }

        public Task GetClanInfoAsync(User player, string argument)
        {
            return Task.CompletedTask;
        }

        public Task GetClanStatsAsync(User player, string argument)
        {
            return Task.CompletedTask;
        }

        public Task GetClientVersionAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task GetMaxMultiplierAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task GetPetAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task GetRestedStatusAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task GetScrollCountAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task GetStreamerTokenCountAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task GetVillageBoostAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task GiftItemAsync(User player, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task InspectPlayerAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task ItemDropEventAsync(User player, string item)
        {
            return Task.CompletedTask;
        }

        public Task JoinArenaAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task JoinAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task JoinClanAsync(User player, string arguments)
        {
            throw new NotImplementedException();
        }

        public Task JoinDungeonAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task JoinDungeonAsync(User player, string code)
        {
            throw new NotImplementedException();
        }

        public Task JoinOnsenAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task JoinRaidAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task JoinRaidAsync(User player, string code)
        {
            throw new NotImplementedException();
        }

        public Task KickAsync(User targetPlayer)
        {
            return Task.CompletedTask;
        }

        public Task KickAsync(User sender, User targetPlayer)
        {
            throw new NotImplementedException();
        }

        public Task KickPlayerFromArenaAsync(User player, User targetPlayer)
        {
            return Task.CompletedTask;
        }

        public Task LeaveArenaAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task LeaveAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task LeaveClanAsync(User player, string argument)
        {
            return Task.CompletedTask;
        }

        public Task LeaveOnsenAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task ObservePlayerAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task Ping(int correlationId)
        {
            return Task.CompletedTask;
        }

        public Task PlayerAppearanceUpdateAsync(User player, string appearance)
        {
            return Task.CompletedTask;
        }

        public Task PlayerCountAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task PlayPetRacingAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task PlayTicTacToeAsync(User player, int num)
        {
            return Task.CompletedTask;
        }

        public async Task<bool> ProcessAsync(int serverPort)
        {
            return true;
        }

        public Task PromoteClanMemberAsync(User player, User targetPlayer, string argument)
        {
            return Task.CompletedTask;
        }

        public Task RaidStartAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task RaidStreamerAsync(User target, bool isRaidWar)
        {
            return Task.CompletedTask;
        }

        public Task RaidStreamerAsync(User player, User target, bool isRaidWar)
        {
            throw new NotImplementedException();
        }

        public Task RedeemStreamerTokenAsync(User player, string query)
        {
            return Task.CompletedTask;
        }

        public Task ReloadGameAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromClanAsync(User player, User targetPlayer, string argument)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromClanAsync(User player, User targetPlayer)
        {
            return Task.CompletedTask;
        }

        public IRavenfallApi this[string correlationid] => Ref(correlationid);
        public IRavenfallApi Ref(string correlationId)
        {
            throw new NotImplementedException();
        }

        public Task RequestHighestSkillAsync(User player, string skill)
        {
            return Task.CompletedTask;
        }

        public Task RequestHighscoreAsync(User player, string skill)
        {
            return Task.CompletedTask;
        }

        public Task RequestIslandInfoAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task RequestPlayerResourcesAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task RequestPlayerStatsAsync(User player, string skill)
        {
            return Task.CompletedTask;
        }

        public Task RequestTrainingInfoAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task ResetPetRacingAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task ResetTicTacToeAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task RestartGameAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task ScalePlayerAsync(User player, float v)
        {
            return Task.CompletedTask;
        }

        public Task SellItemAsync(User player, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task SendClanInviteAsync(User player, User targetPlayer, string argument)
        {
            return Task.CompletedTask;
        }

        public Task SendClanInviteAsync(User player, User targetPlayer)
        {
            return Task.CompletedTask;
        }

        public Task SendPlayerTaskAsync(User player, PlayerTask task, params string[] args)
        {
            return Task.CompletedTask;
        }

        public Task SetAllVillageHutsAsync(User player, string skill)
        {
            return Task.CompletedTask;
        }

        public Task SetExpMultiplierAsync(User player, int number)
        {
            return Task.CompletedTask;
        }

        public Task SetExpMultiplierLimitAsync(User player, int number)
        {
            return Task.CompletedTask;
        }

        public Task SetPetAsync(User player, string pet)
        {
            return Task.CompletedTask;
        }

        public Task SetTimeOfDayAsync(User player, int totalTime, int freezeTime)
        {
            return Task.CompletedTask;
        }

        public Task StartArenaAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task StopDungeonAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task StopRaidAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task ToggleDiaperModeAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task ToggleHelmetAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task ToggleItemRequirementsAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task TogglePetAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task TravelAsync(User player, string destination)
        {
            return Task.CompletedTask;
        }

        public Task TurnIntoMonsterAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task UnequipAsync(User player, string item)
        {
            return Task.CompletedTask;
        }

        public Task UnstuckAsync(User player)
        {
            return Task.CompletedTask;
        }

        public Task UseExpMultiplierScrollAsync(User player, int number)
        {
            return Task.CompletedTask;
        }

        public Task ValueItemAsync(User player, string itemQuery)
        {
            return Task.CompletedTask;
        }

        public Task VendorItemAsync(User player, string itemQuery)
        {
            return Task.CompletedTask;
        }
    }

}