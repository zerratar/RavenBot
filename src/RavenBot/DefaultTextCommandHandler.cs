using RavenBot.Core;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using Shinobytes.Core;

namespace RavenBot
{
    public class DefaultTextCommandHandler : TextCommandHandler
    {
        public DefaultTextCommandHandler(IoC ioc)
            : base(ioc)
        {

            var commandBindingProvider = ioc.Resolve<ICommandBindingProvider>();

#if DEBUG
            Register<DropEventCommandProcessor>(commandBindingProvider.Get("drop"));
            Register<ForceAddPlayerCommandProcessor>(commandBindingProvider.Get("add"));
#endif
            Register<RavenfallInfoCommandProcessor>(commandBindingProvider.Get("ravenfall"));
            Register<ApperanceCommandProcessor>(commandBindingProvider.Get("appearance"));//, "looks");
            Register<StatsCommandProcessor>(commandBindingProvider.Get("stats"));//, "status");
            Register<ResourcesCommandProcessor>(commandBindingProvider.Get("res"));//, "resource", "res");
            Register<HighestSkillCommandProcessor>(commandBindingProvider.Get("highest", "top"));//, "top");
            Register<HighscoreSkillCommandProcessor>(commandBindingProvider.Get("leaderboard", "highscore", "hs"));//, "top");
            Register<DungeonCommandProcessor>(commandBindingProvider.Get("dungeon"));

            Register<ClanCommandProcessor>(commandBindingProvider.Get("clan"));
            

            Register<DuelCommandProcessor>(commandBindingProvider.Get("duel"));//, "fight");
            Register<KickCommandProcessor>(commandBindingProvider.Get("kick"));//);
            Register<ArenaCommandProcessor>(commandBindingProvider.Get("arena"));
            Register<CraftCommandProcessor>(commandBindingProvider.Get("craft"));
            Register<JoinCommandProcessor>(commandBindingProvider.Get("join"));//, "play");

            Register<UnstuckCommandProcessor>(commandBindingProvider.Get("unstuck"));//, "play");

            Register<LeaveCommandProcessor>(commandBindingProvider.Get("leave"));//, "exit", "quit") ;
            Register<IslandInfoCommandProcessor>(commandBindingProvider.Get("where"));//"island", "position", "where"); ;
            Register<TrainingInfoCommandProcessor>(commandBindingProvider.Get("training"));//"skill", "training"); 

            Register<VendorItemCommandProcessor>(commandBindingProvider.Get("vendor"));
            Register<DisenchantItemCommandProcessor>(commandBindingProvider.Get("disenchant"));
            
            Register<GiftItemCommandProcessor>(commandBindingProvider.Get("gift"));
            Register<ValueItemCommandProcessor>(commandBindingProvider.Get("value"));
            Register<CraftRequirementCommandProcessor>(commandBindingProvider.Get("req"));//, "requirement", "requirements");

            Register<ToggleCommandProcessor>(commandBindingProvider.Get("toggle"));
            Register<RavenfallCommandProcessor>(commandBindingProvider.Get("rpg"));
            Register<ObserveCommandProcessor>(commandBindingProvider.Get("show"));//"observe", "show", "display");
            Register<MonsterCommandProcessor>(commandBindingProvider.Get("monster"));

            Register<TinyPlayerCommandProcessor>(commandBindingProvider.Get("small"));
            Register<GiantPlayerCommandProcessor>(commandBindingProvider.Get("big"));

            Register<SetDayCommandProcessor>(commandBindingProvider.Get("day"));
            Register<SetNightCommandProcessor>(commandBindingProvider.Get("night"));

            Register<TravelCommandProcessor>(commandBindingProvider.Get("travel"));
            Register<PlayerCountCommandProcessor>(commandBindingProvider.Get("online"));//, "players");

            Register<PetCommandProcessor>(commandBindingProvider.Get("pet"));
            Register<MultiplierCommandProcessor>(commandBindingProvider.Get("multiplier"));
            Register<ReloadCommandProcessor>(commandBindingProvider.Get("reload"));
            Register<RestartCommandProcessor>(commandBindingProvider.Get("restart"));

            Register<UnequipCommandProcessor>(commandBindingProvider.Get("unequip"));
            Register<EquipCommandProcessor>(commandBindingProvider.Get("equip"));
            Register<EnchantCommandProcessor>(commandBindingProvider.Get("enchant"));

            Register<RaidCommandProcessor>(commandBindingProvider.Get("raid", "raidwar"));

            Register<DiaperModeProcessor>(commandBindingProvider.Get("diaper"));

            Register<InspectCommandProcessor>(commandBindingProvider.Get("inspect"));
            Register<ScrollsCommandProcessor>(commandBindingProvider.Get("scrolls"));
            Register<UseExpMultiplierScrollProcessor>(commandBindingProvider.Get("exp"));

            Register<ToggleItemRequirementsProcessor>("!noitemreq");
            Register<ExpMultiplierProcessor>("!exp");
            Register<ExpMultiplierLimitProcessor>("!explimit");

            Register<StreamerTokenReedeemProcessor>(commandBindingProvider.Get("claim", "redeem"));
            Register<StreamerTokenProcessor>(commandBindingProvider.Get("token", "tokens"));

            Register<TradeItemCommandProcessor>(commandBindingProvider.Get("sell", "buy"));
            Register<TrainCommandProcessor>(commandBindingProvider.Get("train", "task", "strength", "attack", "mine", "defense", "mining", "wood", "crafting", "fishing", "fish", "woodcutting"));
            Register<SailCommandProcessor>(commandBindingProvider.Get("sail", "disembark"));

            Register<OnsenCommandProcessor>(commandBindingProvider.Get("onsen", "rest"));
            Register<RestedCommandProcessor>(commandBindingProvider.Get("rested"));
            Register<VersionCommandProcessor>(commandBindingProvider.Get("version"));
            Register<ItemCountCommandProcessor>(commandBindingProvider.Get("count", "items"));

            Register<VillageCommandProcessor>(commandBindingProvider.Get("village", "town"));

            Register<PubSubActivateCommandProcessor>(commandBindingProvider.Get("pubsub", "channelpointrewards"));//, "points", "rewards"));

            // tavern games
            Register<TicTacToeCommandProcessor>(commandBindingProvider.Get("tictactoe", "ttt"));
            Register<RacingCommandProcessor>(commandBindingProvider.Get("race", "racing"));

            commandBindingProvider.EnsureBindingsFile();
        }
    }
}
