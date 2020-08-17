using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Models;
using RavenBot.Core.Ravenfall.Requests;
using RavenBot.Core.Twitch;

namespace RavenBot.Core.Ravenfall
{
    public class UnityRavenfallClient : IRavenfallClient, IDisposable
    {
        private readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
        private readonly ILogger logger;
        private readonly IMessageBus messageBus;
        private readonly IGameClient client;

        public UnityRavenfallClient(
            ILogger logger,
            IMessageBus messageBus,
            IGameClient2 client)
        {
            this.logger = logger;
            this.messageBus = messageBus;

            messageBus.Subscribe<TwitchUserJoined>(nameof(TwitchUserJoined), OnUserJoined);
            messageBus.Subscribe<TwitchUserLeft>(nameof(TwitchUserLeft), OnUserLeft);
            messageBus.Subscribe<TwitchCheer>(nameof(TwitchCheer), OnUserCheer);
            messageBus.Subscribe<TwitchSubscription>(nameof(TwitchSubscription), OnUserSub);

            this.client = client;
            this.client.Connected += Client_OnConnect;
            this.client.Subscribe("join_failed", OnJoinFailed);
            this.client.Subscribe("join_success", SendResponseToTwitchChat);

            this.client.Subscribe("arena_join_success", SendResponseToTwitchChat);
            this.client.Subscribe("arena_join_failed", SendResponseToTwitchChat);

            this.client.Subscribe("raid_join_success", SendResponseToTwitchChat);
            this.client.Subscribe("raid_join_failed", SendResponseToTwitchChat);
            this.client.Subscribe("raid_start", OnRaidStart);

            this.client.Subscribe("player_stats", SendResponseToTwitchChat);
            this.client.Subscribe("player_resources", SendResponseToTwitchChat);
            this.client.Subscribe("highest_skill", SendResponseToTwitchChat);

            this.client.Subscribe("kick_success", OnKickPlayerSuccess);
            this.client.Subscribe("kick_failed", OnKickPlayerFailed);

            this.client.Subscribe("craft_success", SendResponseToTwitchChat);
            this.client.Subscribe("craft_failed", SendResponseToTwitchChat);

            this.client.Subscribe("duel_failed", SendResponseToTwitchChat);
            this.client.Subscribe("duel_alert", SendResponseToTwitchChat);
            this.client.Subscribe("duel_accept", SendResponseToTwitchChat);
            this.client.Subscribe("duel_declined", SendResponseToTwitchChat);
            this.client.Subscribe("duel_result", SendResponseToTwitchChat);

            this.client.Subscribe("item_pickup", SendResponseToTwitchChat);

            this.client.Subscribe("item_trade_result", SendResponseToTwitchChat);

            this.client.Subscribe("ferry_enter_failed", SendResponseToTwitchChat);
            this.client.Subscribe("ferry_leave_failed", SendResponseToTwitchChat);
            this.client.Subscribe("ferry_travel_failed", SendResponseToTwitchChat);

            this.client.Subscribe("train_failed", SendResponseToTwitchChat);

            this.client.Subscribe("ferry_success", SendResponseToTwitchChat);
            this.client.Subscribe("train_info", SendResponseToTwitchChat);
            this.client.Subscribe("island_info", SendResponseToTwitchChat);

            this.client.Subscribe("message", SendResponseToTwitchChat);
        }

        public Task JoinAsync(Player player) => SendAsync("join", player);

        public Task SetExpMultiplierAsync(Player player, int number) 
            => SendAsync("exp_multiplier", new SetExpMultiplierRequest(player, number));
        public Task DuelRequestAsync(Player challenger, Player target)
            => SendAsync("duel", new DuelPlayerRequest(challenger, target));

        public Task CancelDuelRequestAsync(Player player)
            => SendAsync("duel_cancel", player);

        public Task AcceptDuelRequestAsync(Player player)
            => SendAsync("duel_accept", player);

        public Task DeclineDuelRequestAsync(Player player)
            => SendAsync("duel_decline", player);

        public Task PlayerCountAsync(Player player)
            => SendAsync("player_count", player);

        public Task JoinRaidAsync(Player player)
            => SendAsync("raid_join", player);

        public Task RaidStartAsync(Player player)
            => SendAsync("raid_force", player);

        public Task JoinDungeonAsync(Player player)
            => SendAsync("dungeon_join", player);

        public Task RequestPlayerStatsAsync(Player player, string skill)
            => SendAsync("player_stats", new PlayerStatsRequest(player, skill));

        public Task RequestPlayerResourcesAsync(Player player)
            => SendAsync("player_resources", player);

        public Task RequestHighestSkillAsync(Player player, string skill)
            => SendAsync("highest_skill", new HighestSkillRequest(player, skill));

        public Task PlayerAppearanceUpdateAsync(Player player, string appearance)
            => SendAsync("change_appearance", new PlayerAppearanceRequest(player, appearance));

        public Task ToggleHelmetAsync(Player player)
            => SendAsync("toggle_helmet", player);

        public Task TogglePetAsync(Player player)
            => SendAsync("toggle_pet", player);

        public Task SellItemAsync(Player player, string itemQuery)
            => SendAsync("sell_item", new ItemQueryRequest(player, itemQuery));

        public Task BuyItemAsync(Player player, string itemQuery)
            => SendAsync("buy_item", new ItemQueryRequest(player, itemQuery));

        public Task GiftItemAsync(Player player, string itemQuery)
            => SendAsync("gift_item", new ItemQueryRequest(player, itemQuery));

        public Task VendorItemAsync(Player player, string itemQuery)
            => SendAsync("vendor_item", new ItemQueryRequest(player, itemQuery));

        public Task ValueItemAsync(Player player, string itemQuery)
            => SendAsync("value_item", new ItemQueryRequest(player, itemQuery));

        public Task CraftRequirementAsync(Player player, string itemName)
            => SendAsync("req_item", new ItemQueryRequest(player, itemName));

        public Task SendPlayerTaskAsync(Player player, PlayerTask task, params string[] args)
            => SendAsync("task", new PlayerTaskRequest(player, task.ToString(), args));

        public Task JoinArenaAsync(Player player)
            => SendAsync("arena_join", player);

        public Task LeaveArenaAsync(Player player)
            => SendAsync("arena_leave", player);

        public Task LeaveAsync(Player player)
            => SendAsync("leave", player);

        public Task StartArenaAsync(Player player)
            => SendAsync("arena_begin", player);

        public Task CancelArenaAsync(Player player)
            => SendAsync("arena_end", player);

        public Task KickPlayerFromArenaAsync(Player player, Player targetPlayer)
            => SendAsync("arena_kick", new ArenaKickRequest(player, targetPlayer));

        public Task AddPlayerToArenaAsync(Player player, Player targetPlayer)
            => SendAsync("arena_add", new ArenaAddRequest(player, targetPlayer));

        public Task KickAsync(Player targetPlayer)
            => SendAsync("kick", targetPlayer);

        public Task CraftAsync(Player targetPlayer, string itemQuery)
            => SendAsync("craft", new ItemQueryRequest(targetPlayer, itemQuery));

        public Task TravelAsync(Player player, string destination)
            => SendAsync("ferry_travel", new FerryTravelRequest(player, destination));

        public Task DisembarkFerryAsync(Player player)
            => SendAsync("ferry_leave", player);

        public Task EmbarkFerryAsync(Player player)
            => SendAsync("ferry_enter", player);

        public Task ObservePlayerAsync(Player player)
            => SendAsync("observe", player);

        public Task ItemDropEventAsync(Player player, string item)
            => SendAsync("item_drop_event", new ItemQueryRequest(player, item));

        public Task RequestIslandInfoAsync(Player player)
            => SendAsync("island_info", player);

        public Task RequestTrainingInfoAsync(Player player)
            => SendAsync("train_info", player);

        public Task RaidStreamerAsync(Player target, bool isRaidWar)
            => SendAsync("raid_streamer", new StreamerRaid(target, isRaidWar));

        public Task<bool> ProcessAsync(int serverPort)
            => this.client.ProcessAsync(serverPort);

        public void Dispose()
        {
            this.client.Dispose();
            this.client.Connected -= Client_OnConnect;
        }

        private async void Client_OnConnect(object sender, EventArgs e)
        {
            while (requests.TryDequeue(out var request))
            {
                await this.client.SendAsync(request);
            }
        }
        private async void OnUserCheer(TwitchCheer obj) => await SendAsync("twitch_cheer", obj);
        private async void OnUserSub(TwitchSubscription obj) => await SendAsync("twitch_sub", obj);
        private void OnUserLeft(TwitchUserLeft obj) => logger.WriteDebug(obj.Name + " left the channel");
        private void OnUserJoined(TwitchUserJoined obj) => logger.WriteDebug(obj.Name + " joined the channel");

        private async Task SendAsync<T>(string name, T packet)
        {
            var request = name + ":" + JsonConvert.SerializeObject(packet);

            if (!this.client.IsConnected)
            {
                this.EnqueueRequest(request);
                return;
            }

            await this.client.SendAsync(request);
        }

        private void SendResponseToTwitchChat(IGameCommand obj)
        {
            if (string.IsNullOrEmpty(obj.Destination))
            {
                this.messageBus.Send(MessageBus.Broadcast, obj.Args.LastOrDefault());
            }
            else
            {
                this.messageBus.Send(MessageBus.Message, obj.Destination + ", " + obj.Args.LastOrDefault());
            }
        }

        private void OnRaidStart(IGameCommand obj) => Broadcast(obj);

        private void OnKickPlayerFailed(IGameCommand obj) => Broadcast(obj);

        private void OnKickPlayerSuccess(IGameCommand obj) => Broadcast(obj);

        private void OnJoinFailed(IGameCommand obj)
        {
            this.messageBus.Send(MessageBus.Broadcast, obj.Destination + ", Join failed. Reason: " + obj.Args.LastOrDefault());
        }

        private void Broadcast(IGameCommand obj)
        {
            this.messageBus.Send(MessageBus.Broadcast, obj.Args.LastOrDefault());
        }

        private void EnqueueRequest(string request)
        {
            this.requests.Enqueue(request);
        }
    }
}