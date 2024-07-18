using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Models;
using RavenBot.Core.Ravenfall.Requests;

namespace RavenBot.Core.Ravenfall
{
    public class RavenfallApi : IRavenfallApi
    {
        private IGameClient client;
        private System.Action<string> enqueueRequest;
        private readonly string correlationId;

        public RavenfallApi(IGameClient client, System.Action<string> enqueueRequest, string correlationId)
        {
            this.client = client;
            this.enqueueRequest = enqueueRequest;
            this.correlationId = correlationId;
        }

        public Task UnstuckAsync(User player, string args) => SendAsync("unstuck", player, args);
        public Task JoinAsync(User player) => SendAsync("join", player);
        public Task InspectPlayerAsync(User player) => SendAsync("inspect", player);
        public Task GetStreamerTokenCountAsync(User player) => SendAsync("token_count", player);
        public Task GetScrollCountAsync(User player) => SendAsync("scrolls_count", player);
        public Task RedeemStreamerTokenAsync(User player, string query) => SendAsync("redeem_tokens", player, query);
        public Task PlayTicTacToeAsync(User player, int num) => SendAsync("ttt_play", player, num);
        public Task ActivateTicTacToeAsync(User player) => SendAsync("ttt_activate", player);
        public Task ResetTicTacToeAsync(User player) => SendAsync("ttt_reset", player);
        public Task ResetPetRacingAsync(User player) => SendAsync("pet_race_reset", player);
        public Task PlayPetRacingAsync(User player) => SendAsync("pet_race_play", player);
        public Task GetVillageBoostAsync(User player) => SendAsync("get_village_boost", player);
        public Task ToggleDiaperModeAsync(User player) => SendAsync("toggle_diaper_mode", player);
        public Task ToggleItemRequirementsAsync(User player) => SendAsync("toggle_item_requirements", player);
        public Task SetExpMultiplierAsync(User player, int number) => SendAsync("exp_multiplier", player, number);
        public Task UseExpMultiplierScrollAsync(User player, int number) => SendAsync("use_exp_scroll", player, number);
        public Task SetExpMultiplierLimitAsync(User player, int number) => SendAsync("exp_multiplier_limit", player, number);
        public Task SetTimeOfDayAsync(User player, int totalTime, int freezeTime) => SendAsync("set_time", player, new SetTimeOfDayRequest(totalTime, freezeTime));
        public Task DuelRequestAsync(User player, User target) => SendAsync("duel", player, target);
        public Task CancelDuelRequestAsync(User player) => SendAsync("duel_cancel", player);
        public Task AcceptDuelRequestAsync(User player) => SendAsync("duel_accept", player);
        public Task DeclineDuelRequestAsync(User player) => SendAsync("duel_decline", player);
        public Task PlayerCountAsync(User player) => SendAsync("player_count", player);
        public Task JoinRaidAsync(User player, string query) => SendAsync("raid_join", player, query);
        public Task AutoJoinRaidAsync(User player, string query) => SendAsync("raid_auto", player, query);
        public Task RaidStartAsync(User player) => SendAsync("raid_force", player);
        public Task StopDungeonAsync(User player) => SendAsync("dungeon_stop", player);
        public Task DungeonStartAsync(User player) => SendAsync("dungeon_force", player);
        public Task AutoJoinDungeonAsync(User player, string code) => SendAsync("dungeon_auto", player, code);
        public Task JoinDungeonAsync(User player, string code) => SendAsync("dungeon_join", player, code);
        public Task ReloadGameAsync(User player) => SendAsync("reload", player);
        public Task UpdateGameAsync(User player) => SendAsync("update", player);
        public Task GetMaxMultiplierAsync(User player) => SendAsync("multiplier", player);
        public Task EquipAsync(User player, string pet) => SendAsync("equip", player, pet);
        public Task EnchantAsync(User player, string item) => SendAsync("enchant", player, item);
        public Task DisenchantAsync(User player, string item) => SendAsync("disenchant", player, item);
        public Task ClearEnchantmentCooldownAsync(User player) => SendAsync("clear_enchantment_cooldown", player);
        public Task GetEnchantmentCooldownAsync(User player) => SendAsync("enchantment_cooldown", player);
        public Task CountItemAsync(User player, string item) => SendAsync("get_item_count", player, item);
        public Task ExamineItemAsync(User player, string item) => SendAsync("examine_item", player, item);
        public Task UnequipAsync(User player, string item) => SendAsync("unequip", player, item);
        public Task SetPetAsync(User player, string item) => SendAsync("set_pet", player, item);
        public Task GetPetAsync(User player) => SendAsync("get_pet", player);
        public Task RequestPlayerStatsAsync(User player, string skill) => SendAsync("player_stats", player, skill);

        public Task RequestPlayerEquipmentStatsAsync(User player, string target) => SendAsync("player_eq", player, target);
        public Task RequestPlayerResourcesAsync(User player) => SendAsync("player_resources", player);
        public Task RequestTownResourcesAsync(User player) => SendAsync("town_resources", player);
        public Task ScalePlayerAsync(User player, float scale) => SendAsync("set_player_scale", player, scale);
        public Task RequestHighscoreAsync(User player, string skill) => SendAsync("highscore", player, skill);
        public Task RequestHighestSkillAsync(User player, string skill) => SendAsync("highest_skill", player, skill);
        public Task PlayerAppearanceUpdateAsync(User player, string appearance) => SendAsync("change_appearance", player, appearance);
        public Task ToggleHelmetAsync(User player) => SendAsync("toggle_helmet", player);
        public Task TogglePetAsync(User player) => SendAsync("toggle_pet", player);
        public Task SetAllVillageHutsAsync(User player, string skill) => SendAsync("set_village_huts", player, skill);
        public Task GetVillageStatsAsync(User author) => SendAsync("village_stats", author);
        public Task SellItemAsync(User player, string itemQuery) => SendAsync("sell_item", player, itemQuery);
        public Task BuyItemAsync(User player, string itemQuery) => SendAsync("buy_item", player, itemQuery);
        public Task UseItemAsync(User player, string itemQuery) => SendAsync("use_item", player, itemQuery);
        public Task TeleportAsync(User player, string island) => SendAsync("teleport_island", player, island);

        public Task GetStatusEffectsAsync(User player, string arguments) => SendAsync("get_status_effects", player, arguments);
        public Task UseMarketAsync(User player, string itemQuery) => SendAsync("marketplace", player, itemQuery);
        public Task UseVendorAsync(User player, string itemQuery) => SendAsync("vendor", player, itemQuery);

        public Task GiftItemAsync(User player, string itemQuery) => SendAsync("gift_item", player, itemQuery);
        public Task VendorItemAsync(User player, string itemQuery) => SendAsync("vendor_item", player, itemQuery);
        public Task ValueItemAsync(User player, string itemQuery) => SendAsync("value_item", player, itemQuery);
        public Task CraftRequirementAsync(User player, string itemName) => SendAsync("req_item", player, itemName);
        public Task GetItemUsageAsync(User player, string itemName) => SendAsync("item_usage", player, itemName);
        public Task SendChatMessageAsync(User player, string message) => SendAsync("chat_message", player, message);

        public Task SendPlayerTaskAsync(User player, PlayerTask task, params string[] args) => SendAsync("task", player, new PlayerTaskRequest(task.ToString(), args));

        public Task JoinArenaAsync(User player) => SendAsync("arena_join", player);
        public Task LeaveArenaAsync(User player) => SendAsync("arena_leave", player);
        public Task LeaveAsync(User player) => SendAsync("leave", player);
        public Task StartArenaAsync(User player) => SendAsync("arena_begin", player);
        public Task CancelArenaAsync(User player) => SendAsync("arena_end", player);
        public Task KickPlayerFromArenaAsync(User player, User targetPlayer) => SendAsync("arena_kick", player, targetPlayer);
        public Task AddPlayerToArenaAsync(User player, User targetPlayer) => SendAsync("arena_add", player, targetPlayer);
        public Task KickAsync(User player, User targetPlayer) => SendAsync("kick", player, targetPlayer);

        public Task CraftAsync(User player, string itemQuery) => SendAsync("craft", player, itemQuery);
        public Task CookAsync(User player, string itemQuery) => SendAsync("cook", player, itemQuery);
        public Task FishAsync(User player, string itemQuery) => SendAsync("fish", player, itemQuery);
        public Task MineAsync(User player, string itemQuery) => SendAsync("mine", player, itemQuery);
        public Task FarmAsync(User player, string itemQuery) => SendAsync("farm", player, itemQuery);
        public Task ChopAsync(User player, string itemQuery) => SendAsync("chop", player, itemQuery);
        public Task BrewAsync(User player, string itemQuery) => SendAsync("brew", player, itemQuery);
        public Task GatherAsync(User player, string itemQuery) => SendAsync("gather", player, itemQuery);

        public Task TravelAsync(User player, string destination) => SendAsync("ferry_travel", player, destination);
        public Task DisembarkFerryAsync(User player) => SendAsync("ferry_leave", player);
        public Task EmbarkFerryAsync(User player) => SendAsync("ferry_enter", player);
        public Task ObservePlayerAsync(User player) => SendAsync("observe", player);
        public Task TurnIntoMonsterAsync(User player) => SendAsync("monster", player);
        public Task ItemDropEventAsync(User player, string item) => SendAsync("item_drop_event", player, item);
        public Task RequestIslandInfoAsync(User player) => SendAsync("island_info", player);
        public Task RequestTrainingInfoAsync(User player) => SendAsync("train_info", player);
        public Task RaidStreamerAsync(User player, User target, bool isRaidWar) => SendAsync("raid_streamer", player, new StreamerRaid(target, isRaidWar));
        public Task Ping(int correlationId) => SendAsync("ping", User.ServerRequest, Empty.Shared, correlationId.ToString());
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
        public Task PromoteClanMemberAsync(User player, User targetPlayer, string argument) => SendAsync("clan_promote", player, new Arguments(targetPlayer, argument));
        public Task DemoteClanMemberAsync(User player, User targetPlayer, string argument) => SendAsync("clan_demote", player, new Arguments(targetPlayer, argument));
        public Task AutoRestAsync(User player, int startRestTime, int endRestTime) => SendAsync("auto_rest", player, new Arguments(startRestTime, endRestTime));
        public Task StopAutoRestAsync(User player) => SendAsync("auto_rest_stop", player);
        public Task RequestAutoRestStatusAsync(User player) => SendAsync("auto_rest_status", player);
        public Task AutoUseAsync(User player, int amount) => SendAsync("auto_use", player, amount);
        public Task StopAutoUseAsync(User player) => SendAsync("auto_use_stop", player);
        public Task RequestAutoUseStatusAsync(User player) => SendAsync("auto_use_status", player);
        public Task GetDpsAsync(User player) => SendAsync("dps", player);
        public Task ClearDungeonCombatStyleAsync(User player) => SendAsync("dungeon_skill_clear", player);
        public Task ClearRaidCombatStyleAsync(User player) => SendAsync("raid_skill_clear", player);
        public Task SetRaidCombatStyleAsync(User player, string targetSkill) => SendAsync("raid_skill", player, targetSkill);
        public Task SetDungeonCombatStyleAsync(User player, string targetSkill) => SendAsync("dungeon_skill", player, targetSkill);

        public Task GetLootAsync(User player, string filter) => SendAsync("get_loot", player, filter);

        public Task SendChannelStateAsync(string platform, string channelName, bool inChannel, string message) => SendAsync("channel_state", new Arguments(platform, channelName, inChannel, message));
        public async Task RestartGameAsync(User player)
        {
            await SendAsync("restart", player);

            var ravenfallProcesses = System.Diagnostics.Process.GetProcessesByName("Ravenfall.exe");
            if (ravenfallProcesses.Length == 0)
            {
                //logger.WriteWarning("User issued !restart command but no Ravenfall is seemingly running.");
                return;
            }

            //logger.WriteWarning("Sending restart command to game...");

            var rf = ravenfallProcesses.FirstOrDefault();

            //logger.WriteWarning("Waiting for game to exit..");
            try
            {
                var a = rf.WaitForExitAsync();
                var b = Task.Delay(10000);
                await Task.WhenAny(a, b);
            }
            catch
            {
                //logger.WriteWarning("Error occurred waiting for game to exit. We will wait 5s before attempting to start the game.");
                await Task.Delay(5000);

            }

            if (IsRavenfallRunning())
            {
                //logger.WriteWarning("Game is still running. Unable to restart game");
                return;
            }
            /*
                Now we need to wait for the game to be completely exited so we can try and start it again.
             */

        }

        private bool IsRavenfallRunning()
        {
            var ravenfallProcesses = System.Diagnostics.Process.GetProcessesByName("Ravenfall.exe");
            if (ravenfallProcesses.Length == 0)
            {
                return false;
            }

            return true;
        }

        private Task SendAsync(string type, User sender)
        {
            return SendAsync(type, sender, Empty.Shared);
        }

        private Task SendAsync(string type, object content)
        {
            return SendAsync(type, User.ServerRequest, content);
        }

        private Task SendAsync(string type, User sender, object content)
        {
            return SendAsync(new GameMessage
            {
                Identifier = type,
                Sender = sender,
                Content = JsonConvert.SerializeObject(content),
                CorrelationId = correlationId
            });
        }

        private Task SendAsync(string type, User sender, object content, string correlationId)
        {
            return SendAsync(new GameMessage
            {
                Identifier = type,
                Sender = sender,
                Content = JsonConvert.SerializeObject(content),
                CorrelationId = correlationId
            });
        }

        private async Task SendAsync(GameMessage msg)
        {
            var request = JsonConvert.SerializeObject(msg);
            if (!this.client.IsConnected)
            {
                enqueueRequest(request);
                return;
            }
            await this.client.SendAsync(request);
        }

    }
}
