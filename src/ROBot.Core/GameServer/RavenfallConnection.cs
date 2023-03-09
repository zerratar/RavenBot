using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RavenBot.Core.Chat;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Models;
using RavenBot.Core.Ravenfall.Requests;
using Shinobytes.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ROBot.Core.GameServer
{
    public class RavenfallConnection : IRavenfallConnection
    {
        private readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IBotServer server;
        private readonly IUserProvider playerProvider;
        private readonly IMessageBus messageBus;
        private readonly RavenfallGameClientConnection client;
        private GameSessionInfo queuedSessionInfo;
        private IGameSession session;
        private ITimeoutHandle activePing;

        private IPEndPoint endPoint;

        private int pingSendIndex = 0;
        private int pongReceiveIndex = 0;
        private int missedPingCount = 0;
        private bool disposed;
        private readonly List<IMessageBusSubscription> subs = new List<IMessageBusSubscription>();

        public Guid InstanceId { get; } = Guid.NewGuid();

        public RavenfallConnection(
            ILogger logger,
            IKernel kernel,
            IBotServer server,
            IUserProvider playerProvider,
            IMessageBus messageBus,
            RavenfallGameClientConnection client)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.server = server;
            this.playerProvider = playerProvider;
            this.messageBus = messageBus;

            this.subs.Add(messageBus.Subscribe<UserJoinedEvent>(nameof(UserJoinedEvent), OnUserJoined));
            this.subs.Add(messageBus.Subscribe<UserLeftEvent>(nameof(UserLeftEvent), OnUserLeft));
            this.subs.Add(messageBus.Subscribe<CheerBitsEvent>(nameof(CheerBitsEvent), OnUserCheer));
            this.subs.Add(messageBus.Subscribe<UserSubscriptionEvent>(nameof(UserSubscriptionEvent), OnUserSub));

            this.client = client;
            this.client.Connected += Client_Connected;
            this.client.Disconnected += Client_Disconnected;

            this.client.Subscribe("session", RegisterSession);

            this.client.Subscribe("pong", PongReceived);

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

            if (this.client.IsConnected)
            {
                Client_Connected(this, EventArgs.Empty);
            }
        }


        private event EventHandler<GameSessionInfo> internalSessionInfoReceived;

        public event EventHandler<GameSessionInfo> OnSessionInfoReceived
        {
            add
            {
                internalSessionInfoReceived += value;
                //if (value != null && queuedSessionInfo != null)
                //{
                //    internalSessionInfoReceived.Invoke(this, queuedSessionInfo);
                //}
            }
            remove
            {
                internalSessionInfoReceived -= value;
            }
        }

        public event EventHandler<GameSessionInfo> OnSessionNameChanged;

        public IGameSession Session
        {
            get => session;
            set
            {
                session = value;
                client.Session = value;
            }
        }

        public IPEndPoint EndPoint
        {
            get
            {
                if (endPoint != null || client == null)
                {
                    return endPoint;
                }

                return client.EndPoint;
            }
        }

        public string EndPointString
        {
            get
            {
                try
                {
                    if (client == null)
                    {
                        return "Unknown";
                    }

                    return EndPoint != null ? EndPoint.Address + ":" + EndPoint.Port : "Unknown";
                }
                catch
                {
                    return "Unknown";
                }
            }
        }


        private void PongReceived(IGameCommand obj)
        {
            int.TryParse(obj.Args[0], out pongReceiveIndex);
            missedPingCount = 0;
        }


        private void RegisterSession(IGameCommand obj)
        {
            try
            {
                Guid.TryParse(obj.Args[0], out var sessionid);
                Guid.TryParse(obj.Args[1], out var userId);
                DateTime.TryParse(obj.Args[2], out var sessionStart);

                var player = playerProvider.Get(userId);
                player.IsBroadcaster = true;

                if (internalSessionInfoReceived == null)
                {
                    return;
                }

                internalSessionInfoReceived.Invoke(this, new GameSessionInfo
                {
                    Created = sessionStart,
                    SessionId = sessionid,
                    UserId = userId,
                    Owner = player,
                });
            }
            catch (Exception exc)
            {
                logger.LogError($"RegisterSessionOwner session: {Session?.Name}, failed: " + exc);
            }
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
        public Task SetAllVillageHutsAsync(User player, string skill) => SendAsync("set_village_huts", new PlayerAndString(player, skill));
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
        public Task StopRaidAsync(User player) => SendAsync("raid_stop", player);
        public Task RaidStartAsync(User player) => SendAsync("raid_force", player);
        public Task DungeonStartAsync(User player) => SendAsync("dungeon_force", player);
        public Task StopDungeonAsync(User player) => SendAsync("dungeon_stop", player);
        public Task JoinDungeonAsync(EventJoinRequest player) => SendAsync("dungeon_join", player);
        public Task ReloadGameAsync(User player) => SendAsync("reload", player);
        public Task GetMaxMultiplierAsync(User player) => SendAsync("multiplier", player);
        public Task EquipAsync(User player, string pet) => SendAsync("equip", new ItemQueryRequest(player, pet));
        public Task EnchantAsync(User player, string item) => SendAsync("enchant", new ItemQueryRequest(player, item));
        public Task DisenchantAsync(User player, string item) => SendAsync("disenchant", new ItemQueryRequest(player, item));
        public Task UnequipAsync(User player, string pet) => SendAsync("unequip", new ItemQueryRequest(player, pet));
        public Task SetPetAsync(User player, string pet) => SendAsync("set_pet", new SetPetRequest(player, pet));
        public Task GetPetAsync(User player) => SendAsync("get_pet", new GetPetRequest(player));
        public Task RequestPlayerStatsAsync(User player, string skill) => SendAsync("player_stats", new PlayerStatsRequest(player, skill));
        public Task RequestPlayerResourcesAsync(User player) => SendAsync("player_resources", player);
        public Task ScalePlayerAsync(User player, float scale) => SendAsync("set_player_scale", new SetScaleRequest(player, scale));
        public Task RequestHighscoreAsync(User player, string skill) => SendAsync("highscore", new PlayerStatsRequest(player, skill));
        public Task RequestHighestSkillAsync(User player, string skill) => SendAsync("highest_skill", new HighestSkillRequest(player, skill));
        public Task PlayerAppearanceUpdateAsync(User player, string appearance) => SendAsync("change_appearance", new PlayerAppearanceRequest(player, appearance));
        public Task ToggleHelmetAsync(User player) => SendAsync("toggle_helmet", player);
        public Task TogglePetAsync(User player) => SendAsync("toggle_pet", player);
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
        public Task RestartGameAsync(User player) => SendAsync("restart", player);
        public Task Ping(int correlationId) => SendAsync("ping", new PlayerAndNumber(new User(), correlationId));
        public Task LeaveOnsenAsync(User player) => SendAsync("onsen_leave", player);
        public Task JoinOnsenAsync(User player) => SendAsync("onsen_join", player);
        public Task GetRestedStatusAsync(User player) => SendAsync("rested_status", player);
        public Task GetClientVersionAsync(User player) => SendAsync("client_version", player);
        public Task CountItemAsync(User player, string item) => SendAsync("get_item_count", new ItemQueryRequest(player, item));

        // CLAN
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

        public void Dispose()
        {
            try
            {
                if (!this.disposed)
                {
                    if (subs.Count > 0)
                    {
                        subs.ForEach(x => x.Unsubscribe());
                    }

                    this.client.Connected -= Client_Connected;
                    this.client.Disconnected -= Client_Disconnected;
                    this.client.Dispose();
                    disposed = true;
                    return;
                }
            }
            catch (Exception exc)
            {
                logger.LogError("[RVNFLL] Failed to Dispose Connection: " + exc);
                return;
            }

            logger.LogError("[RVNFLL] Failed to Dispose Connection: Already Disposed");
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            Dispose();
            server.OnClientDisconnected(this);
            if (activePing != null)
            {
                kernel.ClearTimeout(activePing);
            }
        }

        private async void Client_Connected(object sender, EventArgs e)
        {
            //server.OnClientConnected(this);
            this.endPoint = this.client.EndPoint;
            //activePing = kernel.SetTimeout(PingPong, 15000);
            PingPong();

            while (requests.TryDequeue(out var request))
            {
                await this.client.SendAsync(request);
            }
        }

        private void PingPong()
        {
            if (pingSendIndex != pongReceiveIndex)
            {
                logger.LogDebug("[RVNFALL] Connection has not sent any pong back. since last update. Ping " + pingSendIndex + ", Pong " + pongReceiveIndex);
                // Do nothing as of for now. Since clients have not been updated.
                // But otherwise we should have a fail count
                missedPingCount++;
                // and if that goes beyond 2, client should be disconnected. So the game can force reconnect.
                if (missedPingCount > 2)
                {
                    // this.client.Close();
                    // return;
                }
            }

            if (activePing != null)
                kernel.ClearTimeout(activePing);

            Ping(pingSendIndex++);

            activePing = kernel.SetTimeout(() => PingPong(), 3000);
        }

        private async void OnUserCheer(CheerBitsEvent obj)
        {
            if (session == null || !session.Name.Equals(obj.Channel, StringComparison.OrdinalIgnoreCase))
                return;

            logger.LogDebug("[TWITCH] Bits Cheered (Channel: " + obj.Channel + " Bits: " + obj.Bits + " From: " + obj.DisplayName + ")");
            await SendAsync("twitch_cheer", obj);
        }

        private async void OnUserSub(UserSubscriptionEvent obj)
        {
            if (session == null || !session.Name.Equals(obj.Channel, StringComparison.OrdinalIgnoreCase))
                return;

            var name = obj.ReceiverUserId;
            var player = playerProvider.GetByUserId(obj.ReceiverUserId);
            if (player != null)
            {
                name = player.DisplayName;
            }

            logger.LogDebug("[TWITCH] Sub Recieved (Channel: " + obj.Channel + " From: " + obj.DisplayName + " To: " + name + ")");
            await SendAsync("twitch_sub", obj);
        }

        private void OnUserLeft(UserLeftEvent obj) => logger.LogDebug("[TWITCH] " + " User left the channel (User: " + obj.Name + ")");
        private void OnUserJoined(UserJoinedEvent obj) => logger.LogDebug("[TWITCH] " + " User joined the channel (User: " + obj.Name + ")");

        public void Close()
        {
            this.client.Close();
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

        private async Task SendAsync<T>(string name, T packet)
        {
            try
            {
                var request = name + ":" + JsonConvert.SerializeObject(packet);

                if (!this.client.IsConnected)
                {
                    this.EnqueueRequest(request);
                    return;
                }

                await this.client.SendAsync(request);
            }
            catch (Exception exc)
            {
                this.logger.LogError("[RVNFLL] Unable to send packet (" + name + "): to " + this.session?.Name + ", " + exc);
            }
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

        public override string ToString()
        {
            var str = "";
            if (this.session != null)
            {

                str += "Session Name: " + this.session.Name + " ";
            }
            str += "EndPoint: " + EndPointString;

            return str;
        }

    }
}