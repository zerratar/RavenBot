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
            Register<HighestSkillCommandProcessor>(commandBindingProvider.Get("highest"));//, "top");
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
            Register<TravelCommandProcessor>(commandBindingProvider.Get("travel"));
            Register<PlayerCountCommandProcessor>(commandBindingProvider.Get("online"));//, "players");

            Register<PetCommandProcessor>(commandBindingProvider.Get("pet"));
            Register<MultiplierCommandProcessor>(commandBindingProvider.Get("multiplier"));
            Register<ReloadCommandProcessor>(commandBindingProvider.Get("reload"));

            Register<RaidCommandProcessor>("raid", "raidwar");
            Register<ExpMultiplierProcessor>("!exp");
            Register<TradeItemCommandProcessor>("sell", "buy");
            Register<TrainCommandProcessor>("train", "task", "strength", "attack", "mine", "defense", "mining", "wood", "crafting", "fishing", "fish", "woodcutting");
            Register<SailCommandProcessor>("sail", "disembark");

            commandBindingProvider.EnsureBindingsFile();
        }
    }
}
