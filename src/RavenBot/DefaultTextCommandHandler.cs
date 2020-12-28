using RavenBot.Core;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;

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
            Register<HighestSkillCommandProcessor>("highest", "top");//, "top");
            Register<HighscoreSkillCommandProcessor>("leaderboard", "highscore", "hs");//, "top");
            Register<DungeonCommandProcessor>(commandBindingProvider.Get("dungeon"));

            Register<DuelCommandProcessor>(commandBindingProvider.Get("duel"));//, "fight");
            Register<KickCommandProcessor>(commandBindingProvider.Get("kick"));//);
            Register<ArenaCommandProcessor>(commandBindingProvider.Get("arena"));
            Register<CraftCommandProcessor>(commandBindingProvider.Get("craft"));
            Register<JoinCommandProcessor>(commandBindingProvider.Get("join"));//, "play");

            Register<LeaveCommandProcessor>(commandBindingProvider.Get("leave"));//, "exit", "quit") ;
            Register<IslandInfoCommandProcessor>(commandBindingProvider.Get("where"));//"island", "position", "where"); ;
            Register<TrainingInfoCommandProcessor>(commandBindingProvider.Get("training"));//"skill", "training"); 

            Register<VendorItemCommandProcessor>(commandBindingProvider.Get("vendor"));
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

            Register<UnequipCommandProcessor>(commandBindingProvider.Get("unequip"));
            Register<EquipCommandProcessor>(commandBindingProvider.Get("equip"));

            Register<RaidCommandProcessor>("raid", "raidwar");

            Register<DiaperModeProcessor>("diaper");

            Register<InspectCommandProcessor>("inspect");
            Register<ScrollsCommandProcessor>("scrolls");
            Register<UseExpMultiplierScrollProcessor>("exp");

            Register<ToggleItemRequirementsProcessor>("!noitemreq");
            Register<ExpMultiplierProcessor>("!exp");
            Register<ExpMultiplierLimitProcessor>("!explimit");

            Register<StreamerTokenReedeemProcessor>("claim", "redeem");
            Register<StreamerTokenProcessor>("token", "tokens");

            Register<TradeItemCommandProcessor>("sell", "buy");
            Register<TrainCommandProcessor>("train", "task", "strength", "attack", "mine", "defense", "mining", "wood", "crafting", "fishing", "fish", "woodcutting");
            Register<SailCommandProcessor>("sail", "disembark");

            Register<VillageCommandProcessor>("village", "town");

            // tavern games
            Register<TicTacToeCommandProcessor>("tictactoe", "ttt");

            commandBindingProvider.EnsureBindingsFile();
        }
    }
}
