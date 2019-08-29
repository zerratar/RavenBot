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
        }

        private async void OnUserSub(TwitchSubscription obj)
        {
            await SendAsync("twitch_sub", obj);
        }

        private void OnUserLeft(TwitchUserLeft obj)
        {
            logger.WriteDebug(obj.Name + " left the channel");
        }

        private void OnUserJoined(TwitchUserJoined obj)
        {
            logger.WriteDebug(obj.Name + " joined the channel");
        }

        private async void Client_OnConnect(object sender, EventArgs e)
        {
            while (requests.TryDequeue(out var request))
            {
                await this.client.SendAsync(request);
            }
        }

        public Task SendPlayerJoinAsync(Player player)
        {
            return SendAsync("join", player);
        }

        public Task SendDuelRequestAsync(Player challenger, Player target)
        {
            return SendAsync("duel", new DuelPlayerRequest(challenger, target));
        }

        public Task SendCancelDuelRequestAsync(Player player)
        {
            return SendAsync("duel_cancel", player);
        }

        public Task SendAcceptDuelRequestAsync(Player player)
        {
            return SendAsync("duel_accept", player);
        }

        public Task SendDeclineDuelRequestAsync(Player player)
        {
            return SendAsync("duel_decline", player);
        }

        public Task SendPlayerJoinRaidAsync(Player player)
        {
            return SendAsync("raid_join", player);
        }

        public Task SendRaidStartAsync(Player player)
        {
            return SendAsync("raid_force", player);
        }

        public Task SendRequestPlayerStatsAsync(Player player, string skill)
        {
            return SendAsync("player_stats", new PlayerStatsRequest(player, skill));
        }

        public Task SendRequestPlayerResourcesAsync(Player player)
        {
            return SendAsync("player_resources", player);
        }

        public Task SendRequestHighestSkillAsync(Player player, string skill)
        {
            return SendAsync("highest_skill", new HighestSkillRequest(player, skill));
        }

        public Task SendPlayerAppearanceUpdateAsync(Player player, string appearance)
        {
            return SendAsync("change_appearance", new PlayerAppearanceRequest(player, appearance));
        }

        public Task SendToggleHelmetAsync(Player player)
        {
            return SendAsync("toggle_helmet", player);
        }

        public Task SendTogglePetAsync(Player player)
        {
            return SendAsync("toggle_pet", player);
        }

        public Task SendSellItemAsync(Player player, string itemQuery)
        {
            return SendAsync("sell_item", new SellItemRequest(player, itemQuery));
        }

        public Task SendBuyItemAsync(Player player, string itemQuery)
        {
            return SendAsync("buy_item", new BuyItemRequest(player, itemQuery));
        }

        public Task SendPlayerTaskAsync(Player player, PlayerTask task, params string[] args)
        {
            return SendAsync("task", new PlayerTaskRequest(player, task.ToString(), args));
        }

        public Task SendPlayerJoinArenaAsync(Player player)
        {
            return SendAsync("arena_join", player);
        }

        public Task SendPlayerLeaveArenaAsync(Player player)
        {
            return SendAsync("arena_leave", player);
        }

        public Task SendStartArenaAsync(Player player)
        {
            return SendAsync("arena_begin", player);
        }

        public Task SendCancelArenaAsync(Player player)
        {
            return SendAsync("arena_end", player);
        }

        public Task SendKickPlayerFromArenaAsync(Player player, Player targetPlayer)
        {
            return SendAsync("arena_kick", new ArenaKickRequest(player, targetPlayer));
        }

        public Task SendAddPlayerToArenaAsync(Player player, Player targetPlayer)
        {
            return SendAsync("arena_add", new ArenaAddRequest(player, targetPlayer));
        }

        public Task SendKickPlayerAsync(Player targetPlayer)
        {
            return SendAsync("kick", targetPlayer);
        }

        public Task SendCraftAsync(Player targetPlayer, string itemCategory, string itemType)
        {
            return SendAsync("craft", new CraftRequest(targetPlayer, itemCategory, itemType));
        }

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

        private void OnRaidStart(IGameCommand obj)
        {
            this.messageBus.Send(MessageBus.Broadcast, obj.Args.LastOrDefault());
        }

        private void OnKickPlayerFailed(IGameCommand obj)
        {
            this.messageBus.Send(MessageBus.Broadcast, obj.Args.LastOrDefault());
        }

        private void OnKickPlayerSuccess(IGameCommand obj)
        {
            this.messageBus.Send(MessageBus.Broadcast, obj.Args.LastOrDefault());
        }

        private void OnJoinFailed(IGameCommand obj)
        {
            this.messageBus.Send(MessageBus.Broadcast, obj.Destination + ", Join failed. Reason: " + obj.Args.LastOrDefault());
        }

        private void EnqueueRequest(string request)
        {
            this.requests.Enqueue(request);
        }

        public void Dispose()
        {
            this.client.Dispose();
            this.client.Connected -= Client_OnConnect;
        }

        public Task<bool> ProcessAsync(int serverPort)
        {
            return this.client.ProcessAsync(serverPort);
        }
    }
}