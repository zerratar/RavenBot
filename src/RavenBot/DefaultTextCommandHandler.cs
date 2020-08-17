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

#if DEBUG
            Register<DropEventCommandProcessor>("drop");
            Register<ForceAddPlayerCommandProcessor>("add");
#endif

            Register<RavenfallInfoCommandProcessor>("ravenfall");

            Register<ApperanceCommandProcessor>("appearance", "looks");
            Register<StatsCommandProcessor>("stats", "status");
            Register<ResourcesCommandProcessor>("resources", "resource", "res");
            Register<HighestSkillCommandProcessor>("highest", "top");
            Register<RaidCommandProcessor>("raid", "raidwar");
            Register<DungeonCommandProcessor>("dungeon");

            Register<DuelCommandProcessor>("duel", "fight");
            Register<KickCommandProcessor>("kick");
            Register<ArenaCommandProcessor>("arena");
            Register<CraftCommandProcessor>("craft");
            Register<JoinCommandProcessor>("join", "play");
            Register<ExpMultiplierProcessor>("!exp");
            Register<LeaveCommandProcessor>("leave", "exit", "quit");

            Register<IslandInfoCommandProcessor>("island", "position", "where");
            Register<TrainingInfoCommandProcessor>("skill", "training");

            Register<TradeItemCommandProcessor>("sell", "buy");
            Register<VendorItemCommandProcessor>("vendor");
            Register<GiftItemCommandProcessor>("gift");
            Register<ValueItemCommandProcessor>("value");
            Register<CraftRequirementCommandProcessor>("req", "requirement", "requirements");

            Register<ToggleCommandProcessor>("toggle");
            Register<TrainCommandProcessor>("train", "task", "strength", "attack", "mine", "defense", "mining", "wood", "crafting", "fishing", "fish", "woodcutting");
            Register<RavenfallCommandProcessor>("rpg");

            Register<ObserveCommandProcessor>("observe", "show", "display");

            Register<SailCommandProcessor>("sail", "disembark");
            Register<TravelCommandProcessor>("travel");

            Register<PlayerCountCommandProcessor>("online", "players");
        }
    }
}
