using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Ravenfall.Models;
using RavenBot.Core.Ravenfall.Requests;
using RavenBot.Core.Twitch;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace ROBot.Core.GameServer
{
    public class RavenfallConnection : IRavenfallConnection
    {
        private readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
        private readonly ILogger logger;
        private readonly IBotServer server;
        private readonly IPlayerProvider playerProvider;
        private readonly IMessageBus messageBus;
        private readonly RavenfallGameClientConnection client;
        private GameSessionInfo queuedSessionInfo;
        private IGameSession session;

        public RavenfallConnection(
            ILogger logger,
            IBotServer server,
            IPlayerProvider playerProvider,
            IMessageBus messageBus,
            RavenfallGameClientConnection client)
        {
            this.logger = logger;
            this.server = server;
            this.playerProvider = playerProvider;
            this.messageBus = messageBus;

            messageBus.Subscribe<ROBot.Core.Twitch.TwitchUserJoined>(nameof(TwitchUserJoined), OnUserJoined);
            messageBus.Subscribe<ROBot.Core.Twitch.TwitchUserLeft>(nameof(TwitchUserLeft), OnUserLeft);
            messageBus.Subscribe<ROBot.Core.Twitch.TwitchCheer>(nameof(TwitchCheer), OnUserCheer);
            messageBus.Subscribe<ROBot.Core.Twitch.TwitchSubscription>(nameof(TwitchSubscription), OnUserSub);

            this.client = client;
            this.client.Connected += Client_Connected;
            this.client.Disconnected += Client_Disconnected;

            this.client.Subscribe("session_owner", RegisterSessionOwner);

            this.client.Subscribe("join_failed", SendResponseToTwitchChat);
            this.client.Subscribe("join_success", SendResponseToTwitchChat);

            this.client.Subscribe("arena_join_success", SendResponseToTwitchChat);
            this.client.Subscribe("arena_join_failed", SendResponseToTwitchChat);

            this.client.Subscribe("raid_join_success", SendResponseToTwitchChat);
            this.client.Subscribe("raid_join_failed", SendResponseToTwitchChat);
            this.client.Subscribe("raid_start", SendResponseToTwitchChat);

            this.client.Subscribe("player_stats", SendResponseToTwitchChat);
            this.client.Subscribe("player_resources", SendResponseToTwitchChat);
            this.client.Subscribe("highest_skill", SendResponseToTwitchChat);

            this.client.Subscribe("kick_success", SendResponseToTwitchChat);
            this.client.Subscribe("kick_failed", SendResponseToTwitchChat);

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

        private event EventHandler<GameSessionInfo> internalSessionInfoReceived;

        public event EventHandler<GameSessionInfo> OnSessionInfoReceived
        {
            add
            {
                internalSessionInfoReceived += value;
                if (value != null && queuedSessionInfo != null)
                {
                    internalSessionInfoReceived.Invoke(this, queuedSessionInfo);
                }
            }
            remove
            {
                internalSessionInfoReceived -= value;
            }
        }

        public IGameSession Session
        {
            get => session;
            set
            {
                session = value;
                client.Session = value;
            }
        }

        public IPEndPoint EndPoint => client.EndPoint;
        public string EndPointString => EndPoint != null ? EndPoint.Address + ":" + EndPoint.Port : "Unknown";
        private void RegisterSessionOwner(IGameCommand obj)
        {
            if (string.IsNullOrEmpty(obj.Args[0]))
                return;

            Guid sessionId = Guid.Empty;
            if (obj.Args.Length > 2)
            {
                Guid.TryParse(obj.Args[2], out sessionId);
            }
            var plr = playerProvider.Get(obj.Args[0], obj.Args[1]);
            plr.IsBroadcaster = true;

            var sessionInfo = new GameSessionInfo
            {
                SessionId = sessionId,
                TwitchUserId = plr.UserId,
                TwitchUserName = plr.Username
            };

            if (internalSessionInfoReceived == null)
            {
                queuedSessionInfo = sessionInfo;
            }
            else
            {
                queuedSessionInfo = null;
                internalSessionInfoReceived.Invoke(this, sessionInfo);
            }
        }

        public Task UnstuckAsync(Player player) => SendAsync("unstuck", player);
        public Task JoinAsync(Player player) => SendAsync("join", player);
        public Task InspectPlayerAsync(Player player) => SendAsync("inspect", player);
        public Task GetStreamerTokenCountAsync(Player player) => SendAsync("token_count", player);
        public Task GetScrollCountAsync(Player player) => SendAsync("scrolls_count", player);
        public Task RedeemStreamerTokenAsync(Player player, string query) => SendAsync("redeem_tokens", new ItemQueryRequest(player, query));
        public Task PlayTicTacToeAsync(Player player, int num) => SendAsync("ttt_play", new PlayerAndNumber(player, num));
        public Task ActivateTicTacToeAsync(Player player) => SendAsync("ttt_activate", player);
        public Task ResetTicTacToeAsync(Player player) => SendAsync("ttt_reset", player);
        public Task ResetPetRacingAsync(Player player) => SendAsync("pet_race_reset", player);
        public Task PlayPetRacingAsync(Player player) => SendAsync("pet_race_play", player);
        public Task GetVillageBoostAsync(Player player) => SendAsync("get_village_boost", player);
        public Task ToggleDiaperModeAsync(Player player) => SendAsync("toggle_diaper_mode", player);
        public Task ToggleItemRequirementsAsync(Player player) => SendAsync("toggle_item_requirements", player);
        public Task SetExpMultiplierAsync(Player player, int number) => SendAsync("exp_multiplier", new SetExpMultiplierRequest(player, number));
        public Task UseExpMultiplierScrollAsync(Player player, int number) => SendAsync("use_exp_scroll", new SetExpMultiplierRequest(player, number));
        public Task SetExpMultiplierLimitAsync(Player player, int number) => SendAsync("exp_multiplier_limit", new SetExpMultiplierRequest(player, number));
        public Task SetTimeOfDayAsync(Player player, int totalTime, int freezeTime) => SendAsync("set_time", new SetTimeOfDayRequest(player, totalTime, freezeTime));
        public Task DuelRequestAsync(Player challenger, Player target) => SendAsync("duel", new DuelPlayerRequest(challenger, target));
        public Task CancelDuelRequestAsync(Player player) => SendAsync("duel_cancel", player);
        public Task AcceptDuelRequestAsync(Player player) => SendAsync("duel_accept", player);
        public Task DeclineDuelRequestAsync(Player player) => SendAsync("duel_decline", player);
        public Task PlayerCountAsync(Player player) => SendAsync("player_count", player);
        public Task JoinRaidAsync(Player player) => SendAsync("raid_join", player);
        public Task RaidStartAsync(Player player) => SendAsync("raid_force", player);
        public Task DungeonStartAsync(Player player) => SendAsync("dungeon_force", player);
        public Task JoinDungeonAsync(Player player) => SendAsync("dungeon_join", player);
        public Task ReloadGameAsync(Player player) => SendAsync("reload", player);
        public Task GetMaxMultiplierAsync(Player player) => SendAsync("multiplier", player);
        public Task EquipAsync(Player player, string pet) => SendAsync("equip", new ItemQueryRequest(player, pet));
        public Task UnequipAsync(Player player, string pet) => SendAsync("unequip", new ItemQueryRequest(player, pet));
        public Task SetPetAsync(Player player, string pet) => SendAsync("set_pet", new SetPetRequest(player, pet));
        public Task GetPetAsync(Player player) => SendAsync("get_pet", new GetPetRequest(player));
        public Task RequestPlayerStatsAsync(Player player, string skill) => SendAsync("player_stats", new PlayerStatsRequest(player, skill));
        public Task RequestPlayerResourcesAsync(Player player) => SendAsync("player_resources", player);
        public Task ScalePlayerAsync(Player player, float scale) => SendAsync("set_player_scale", new SetScaleRequest(player, scale));
        public Task RequestHighscoreAsync(Player player, string skill) => SendAsync("highscore", new PlayerStatsRequest(player, skill));
        public Task RequestHighestSkillAsync(Player player, string skill) => SendAsync("highest_skill", new HighestSkillRequest(player, skill));
        public Task PlayerAppearanceUpdateAsync(Player player, string appearance) => SendAsync("change_appearance", new PlayerAppearanceRequest(player, appearance));
        public Task ToggleHelmetAsync(Player player) => SendAsync("toggle_helmet", player);
        public Task TogglePetAsync(Player player) => SendAsync("toggle_pet", player);
        public Task SellItemAsync(Player player, string itemQuery) => SendAsync("sell_item", new ItemQueryRequest(player, itemQuery));
        public Task BuyItemAsync(Player player, string itemQuery) => SendAsync("buy_item", new ItemQueryRequest(player, itemQuery));
        public Task GiftItemAsync(Player player, string itemQuery) => SendAsync("gift_item", new ItemQueryRequest(player, itemQuery));
        public Task VendorItemAsync(Player player, string itemQuery) => SendAsync("vendor_item", new ItemQueryRequest(player, itemQuery));
        public Task ValueItemAsync(Player player, string itemQuery) => SendAsync("value_item", new ItemQueryRequest(player, itemQuery));
        public Task CraftRequirementAsync(Player player, string itemName) => SendAsync("req_item", new ItemQueryRequest(player, itemName));
        public Task SendPlayerTaskAsync(Player player, PlayerTask task, params string[] args) => SendAsync("task", new PlayerTaskRequest(player, task.ToString(), args));
        public Task JoinArenaAsync(Player player) => SendAsync("arena_join", player);
        public Task LeaveArenaAsync(Player player) => SendAsync("arena_leave", player);
        public Task LeaveAsync(Player player) => SendAsync("leave", player);
        public Task StartArenaAsync(Player player) => SendAsync("arena_begin", player);
        public Task CancelArenaAsync(Player player) => SendAsync("arena_end", player);
        public Task KickPlayerFromArenaAsync(Player player, Player targetPlayer) => SendAsync("arena_kick", new ArenaKickRequest(player, targetPlayer));
        public Task AddPlayerToArenaAsync(Player player, Player targetPlayer) => SendAsync("arena_add", new ArenaAddRequest(player, targetPlayer));
        public Task KickAsync(Player targetPlayer) => SendAsync("kick", targetPlayer);
        public Task CraftAsync(Player targetPlayer, string itemQuery) => SendAsync("craft", new ItemQueryRequest(targetPlayer, itemQuery));
        public Task TravelAsync(Player player, string destination) => SendAsync("ferry_travel", new FerryTravelRequest(player, destination));
        public Task DisembarkFerryAsync(Player player) => SendAsync("ferry_leave", player);
        public Task EmbarkFerryAsync(Player player) => SendAsync("ferry_enter", player);
        public Task ObservePlayerAsync(Player player) => SendAsync("observe", player);
        public Task TurnIntoMonsterAsync(Player player) => SendAsync("monster", player);
        public Task ItemDropEventAsync(Player player, string item) => SendAsync("item_drop_event", new ItemQueryRequest(player, item));
        public Task RequestIslandInfoAsync(Player player) => SendAsync("island_info", player);
        public Task RequestTrainingInfoAsync(Player player) => SendAsync("train_info", player);
        public Task RaidStreamerAsync(Player target, bool isRaidWar) => SendAsync("raid_streamer", new StreamerRaid(target, isRaidWar));
        public Task RestartGameAsync(Player player) => SendAsync("restart", player);

        public void Dispose()
        {
            this.client.Connected -= Client_Connected;
            this.client.Disconnected -= Client_Disconnected;
            this.client.Dispose();
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            server.OnClientDisconnected(this);
        }

        private async void Client_Connected(object sender, EventArgs e)
        {
            //server.OnClientConnected(this);
            while (requests.TryDequeue(out var request))
            {
                await this.client.SendAsync(request);
            }
        }

        private async void OnUserCheer(ROBot.Core.Twitch.TwitchCheer obj)
        {
            if (session == null || !session.Name.Equals(obj.Channel, StringComparison.OrdinalIgnoreCase))
                return;

            await SendAsync("twitch_cheer", obj);
        }

        private async void OnUserSub(ROBot.Core.Twitch.TwitchSubscription obj)
        {
            if (session == null || !session.Name.Equals(obj.Channel, StringComparison.OrdinalIgnoreCase))
                return;

            await SendAsync("twitch_sub", obj);
        }

        private void OnUserLeft(ROBot.Core.Twitch.TwitchUserLeft obj) => logger.LogDebug(obj.Name + " left the channel");
        private void OnUserJoined(ROBot.Core.Twitch.TwitchUserJoined obj) => logger.LogDebug(obj.Name + " joined the channel");

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
            this.messageBus.Send(MessageBus.Broadcast, obj);
        }

        private void EnqueueRequest(string request)
        {
            this.requests.Enqueue(request);
        }

        public Task<bool> ProcessAsync(int serverPort)
        {
            return Task.FromResult(true);
        }
    }
}