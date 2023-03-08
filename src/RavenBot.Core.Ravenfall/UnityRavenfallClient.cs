using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Ravenfall.Models;
using RavenBot.Core.Ravenfall.Requests;
using RavenBot.Core.Twitch;

namespace RavenBot.Core.Ravenfall
{
    public class UnityRavenfallClient : IRavenfallClient, IDisposable
    {
        private readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
        private readonly ILogger logger;
        private readonly IUserProvider playerProvider;
        private readonly IMessageBus messageBus;
        private readonly IGameClient client;

        public UnityRavenfallClient(
            ILogger logger,
            IUserProvider playerProvider,
            IMessageBus messageBus,
            IGameClient2 client)
        {
            this.logger = logger;
            this.playerProvider = playerProvider;
            this.messageBus = messageBus;

            messageBus.Subscribe<UserJoinedEvent>(nameof(UserJoinedEvent), OnUserJoined);
            messageBus.Subscribe<UserLeftEvent>(nameof(UserLeftEvent), OnUserLeft);
            messageBus.Subscribe<CheerBitsEvent>(nameof(CheerBitsEvent), OnUserCheer);
            messageBus.Subscribe<UserSubscriptionEvent>(nameof(UserSubscriptionEvent), OnUserSub);

            this.client = client;
            this.client.Connected += Client_OnConnect;

            this.client.Subscribe("session_owner", RegisterSessionOwner);
            this.client.Subscribe("pubsub_token", RegisterPubSubToken);

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

        private void RegisterPubSubToken(IGameCommand obj)
        {
            var userId = obj.Args[0];
            var username = obj.Args[1];
            var token = obj.Args[2];

            messageBus.Send("pubsub_token", userId + "," + token);
        }

        private void RegisterSessionOwner(IGameCommand obj)
        {
            if (string.IsNullOrEmpty(obj.Args[0]))
                return;

            var plr = playerProvider.Get(obj.Args[0], obj.Args[1]);
            plr.IsBroadcaster = true;

            messageBus.Send("streamer_userid_acquired", plr.PlatformId);
        }

        public Task UnstuckAsync(User player) => SendAsync("unstuck", player);
        public Task JoinAsync(User player) => SendAsync("join", player);
        public Task InspectPlayerAsync(User player) => SendAsync("inspect", player);
        public Task GetStreamerTokenCountAsync(User player) => SendAsync("token_count", player);
        public Task GetScrollCountAsync(User player) => SendAsync("scrolls_count", player);
        public Task RedeemStreamerTokenAsync(User player, string query) => SendAsync("redeem_tokens", new ItemQueryRequest(player, query));
        public Task PlayTicTacToeAsync(User player, int num) => SendAsync("ttt_play", new PlayerAndNumber(player, num));
        public Task ActivateTicTacToeAsync(User player) => SendAsync("ttt_activate", player);
        public Task ResetTicTacToeAsync(User player) => SendAsync("ttt_reset", player);
        public Task ResetPetRacingAsync(User player) => SendAsync("pet_race_reset", player);
        public Task PlayPetRacingAsync(User player) => SendAsync("pet_race_play", player);
        public Task GetVillageBoostAsync(User player) => SendAsync("get_village_boost", player);
        public Task ToggleDiaperModeAsync(User player) => SendAsync("toggle_diaper_mode", player);
        public Task ToggleItemRequirementsAsync(User player) => SendAsync("toggle_item_requirements", player);
        public Task SetExpMultiplierAsync(User player, int number) => SendAsync("exp_multiplier", new SetExpMultiplierRequest(player, number));
        public Task UseExpMultiplierScrollAsync(User player, int number) => SendAsync("use_exp_scroll", new SetExpMultiplierRequest(player, number));
        public Task SetExpMultiplierLimitAsync(User player, int number) => SendAsync("exp_multiplier_limit", new SetExpMultiplierRequest(player, number));
        public Task SetTimeOfDayAsync(User player, int totalTime, int freezeTime) => SendAsync("set_time", new SetTimeOfDayRequest(player, totalTime, freezeTime));
        public Task DuelRequestAsync(User challenger, User target) => SendAsync("duel", new DuelPlayerRequest(challenger, target));
        public Task CancelDuelRequestAsync(User player) => SendAsync("duel_cancel", player);
        public Task AcceptDuelRequestAsync(User player) => SendAsync("duel_accept", player);
        public Task DeclineDuelRequestAsync(User player) => SendAsync("duel_decline", player);
        public Task PlayerCountAsync(User player) => SendAsync("player_count", player);
        public Task JoinRaidAsync(EventJoinRequest player) => SendAsync("raid_join", player);
        public Task RaidStartAsync(User player) => SendAsync("raid_force", player);
        public Task StopDungeonAsync(User player) => SendAsync("dungeon_stop", player);
        public Task DungeonStartAsync(User player) => SendAsync("dungeon_force", player);
        public Task JoinDungeonAsync(EventJoinRequest player) => SendAsync("dungeon_join", player);
        public Task ReloadGameAsync(User player) => SendAsync("reload", player);
        public Task GetMaxMultiplierAsync(User player) => SendAsync("multiplier", player);
        public Task EquipAsync(User player, string pet) => SendAsync("equip", new ItemQueryRequest(player, pet));
        public Task EnchantAsync(User player, string item) => SendAsync("enchant", new ItemQueryRequest(player, item));
        public Task DisenchantAsync(User player, string item) => SendAsync("disenchant", new ItemQueryRequest(player, item));
        public Task CountItemAsync(User player, string item) => SendAsync("get_item_count", new ItemQueryRequest(player, item));
        public Task UnequipAsync(User player, string item) => SendAsync("unequip", new ItemQueryRequest(player, item));
        public Task SetPetAsync(User player, string item) => SendAsync("set_pet", new SetPetRequest(player, item));
        public Task GetPetAsync(User player) => SendAsync("get_pet", new GetPetRequest(player));
        public Task RequestPlayerStatsAsync(User player, string skill) => SendAsync("player_stats", new PlayerStatsRequest(player, skill));
        public Task RequestPlayerResourcesAsync(User player) => SendAsync("player_resources", player);
        public Task ScalePlayerAsync(User player, float scale) => SendAsync("set_player_scale", new SetScaleRequest(player, scale));
        public Task RequestHighscoreAsync(User player, string skill) => SendAsync("highscore", new PlayerStatsRequest(player, skill));
        public Task RequestHighestSkillAsync(User player, string skill) => SendAsync("highest_skill", new HighestSkillRequest(player, skill));
        public Task PlayerAppearanceUpdateAsync(User player, string appearance) => SendAsync("change_appearance", new PlayerAppearanceRequest(player, appearance));
        public Task ToggleHelmetAsync(User player) => SendAsync("toggle_helmet", player);
        public Task TogglePetAsync(User player) => SendAsync("toggle_pet", player);
        public Task SetAllVillageHutsAsync(User player, string skill) => SendAsync("set_village_huts", new PlayerAndString(player, skill));
        public Task SellItemAsync(User player, string itemQuery) => SendAsync("sell_item", new ItemQueryRequest(player, itemQuery));
        public Task BuyItemAsync(User player, string itemQuery) => SendAsync("buy_item", new ItemQueryRequest(player, itemQuery));
        public Task GiftItemAsync(User player, string itemQuery) => SendAsync("gift_item", new ItemQueryRequest(player, itemQuery));
        public Task VendorItemAsync(User player, string itemQuery) => SendAsync("vendor_item", new ItemQueryRequest(player, itemQuery));
        public Task ValueItemAsync(User player, string itemQuery) => SendAsync("value_item", new ItemQueryRequest(player, itemQuery));
        public Task CraftRequirementAsync(User player, string itemName) => SendAsync("req_item", new ItemQueryRequest(player, itemName));
        public Task SendPlayerTaskAsync(User player, PlayerTask task, params string[] args) => SendAsync("task", new PlayerTaskRequest(player, task.ToString(), args));
        public Task JoinArenaAsync(User player) => SendAsync("arena_join", player);
        public Task LeaveArenaAsync(User player) => SendAsync("arena_leave", player);
        public Task LeaveAsync(User player) => SendAsync("leave", player);
        public Task StartArenaAsync(User player) => SendAsync("arena_begin", player);
        public Task CancelArenaAsync(User player) => SendAsync("arena_end", player);
        public Task KickPlayerFromArenaAsync(User player, User targetPlayer) => SendAsync("arena_kick", new PlayerAndPlayer(player, targetPlayer));
        public Task AddPlayerToArenaAsync(User player, User targetPlayer) => SendAsync("arena_add", new ArenaAddRequest(player, targetPlayer));
        public Task KickAsync(User targetPlayer) => SendAsync("kick", targetPlayer);
        public Task CraftAsync(User targetPlayer, string itemQuery) => SendAsync("craft", new ItemQueryRequest(targetPlayer, itemQuery));
        public Task TravelAsync(User player, string destination) => SendAsync("ferry_travel", new FerryTravelRequest(player, destination));
        public Task DisembarkFerryAsync(User player) => SendAsync("ferry_leave", player);
        public Task EmbarkFerryAsync(User player) => SendAsync("ferry_enter", player);
        public Task ObservePlayerAsync(User player) => SendAsync("observe", player);
        public Task TurnIntoMonsterAsync(User player) => SendAsync("monster", player);
        public Task ItemDropEventAsync(User player, string item) => SendAsync("item_drop_event", new ItemQueryRequest(player, item));
        public Task RequestIslandInfoAsync(User player) => SendAsync("island_info", player);
        public Task RequestTrainingInfoAsync(User player) => SendAsync("train_info", player);
        public Task RaidStreamerAsync(User target, bool isRaidWar) => SendAsync("raid_streamer", new StreamerRaid(target, isRaidWar));
        public Task Ping(int correlationId) => SendAsync("ping", new PlayerAndNumber(new User(), correlationId));
        public Task LeaveOnsenAsync(User player) => SendAsync("onsen_leave", player);
        public Task JoinOnsenAsync(User player) => SendAsync("onsen_join", player);
        public Task GetRestedStatusAsync(User player) => SendAsync("rested_status", player);
        public Task StopRaidAsync(User player) => SendAsync("raid_stop", player);
        public Task GetClientVersionAsync(User player) => SendAsync("client_version", player);

        public Task GetClanInfoAsync(User player, string arg) => SendAsync("clan_info", player, arg);
        public Task GetClanStatsAsync(User player, string arg) => SendAsync("clan_stats", player, arg);
        public Task JoinClanAsync(User player, string arguments) => SendAsync("clan_join", player, arguments);
        public Task LeaveClanAsync(User player, string argument) => SendAsync("clan_leave", player, argument);
        public Task RemoveFromClanAsync(User player, User targetPlayer) => SendAsync("clan_remove", player, targetPlayer);
        public Task SendClanInviteAsync(User player, User targetPlayer) => SendAsync("clan_invite", player, targetPlayer);
        public Task AcceptClanInviteAsync(User player, string argument) => SendAsync("clan_accept", player, argument);
        public Task DeclineClanInviteAsync(User player, string argument) => SendAsync("clan_decline", player, argument);
        public Task PromoteClanMemberAsync(User player, User targetPlayer, string argument) => SendAsync("clan_promote", player, targetPlayer, argument);
        public Task DemoteClanMemberAsync(User player, User targetPlayer, string argument) => SendAsync("clan_demote", player, targetPlayer, argument);

        public async Task RestartGameAsync(User player)
        {
            string gamePath = GetRavenfallGamePath();
            if (string.IsNullOrEmpty(gamePath) || !System.IO.File.Exists(gamePath))
            {
                return;
            }

            await SendAsync("restart", player);
            await WaitForGameToExit();
            StartRavenfall(gamePath);
        }

        private void StartRavenfall(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(path);
            }
            catch { }
        }

        private async Task WaitForGameToExit()
        {
            var ravenfall = System.Diagnostics.Process.GetProcessesByName("ravenfall").FirstOrDefault();
            if (ravenfall == null || ravenfall.HasExited)
            {
                return;
            }
            try
            {
                await ravenfall.WaitForExitAsync();

                while (!ravenfall.HasExited)
                {
                    await Task.Delay(500);
                    ravenfall.Refresh();
                }
            }
            catch
            {
                logger.WriteError("Error when waiting for ravenfall to exit.");
            }
        }

        private static string GetRavenfallGamePath()
        {
            var ravenfall = System.Diagnostics.Process.GetProcessesByName("Ravenfall").FirstOrDefault();
            if (ravenfall == null)
            {
                if (System.IO.File.Exists("ravenfall.exe"))
                {
                    return "ravenfall.exe";
                }
            }

            return ravenfall.MainModule.FileName;
        }

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
        private async void OnUserCheer(CheerBitsEvent obj) => await SendAsync("twitch_cheer", obj);
        private async void OnUserSub(UserSubscriptionEvent obj) => await SendAsync("twitch_sub", obj);
        private void OnUserLeft(UserLeftEvent obj) => logger.WriteMessage(obj.Name + " left the channel");
        private void OnUserJoined(UserJoinedEvent obj) => logger.WriteMessage(obj.Name + " joined the channel");

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

        private Task SendAsync(string name, User player, User arg0, string arg1)
        {
            return SendAsync(name, new PlayerPlayerAndString(player, arg0, arg1));
        }

        private Task SendAsync(string name, User player, User argument)
        {
            return SendAsync(name, new PlayerAndPlayer(player, argument));
        }

        private Task SendAsync(string name, User player, string argument)
        {
            return SendAsync(name, new PlayerAndString(player, argument));
        }

        private Task SendAsync(string name, User player, int argument)
        {
            return SendAsync(name, new PlayerAndNumber(player, argument));
        }

        private void SendResponseToTwitchChat(IGameCommand obj)
        {
            this.messageBus.Send(MessageBus.Broadcast, obj);
        }

        private void EnqueueRequest(string request)
        {
            this.requests.Enqueue(request);
        }

    }

    //public class BroadcastMessage
    //{
    //    public string User { get; set; }
    //    public string Message { get; set; }
    //}
}