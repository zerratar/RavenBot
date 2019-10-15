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
            Register<ForceAddPlayerCommandProcessor>("add");
#endif

            Register<ApperanceCommandProcessor>("appearance", "looks");
            Register<StatsCommandProcessor>("stats", "status");
            Register<ResourcesCommandProcessor>("resources", "resource", "res");
            Register<HighestSkillCommandProcessor>("highest");
            Register<RaidCommandProcessor>("raid", "raidwar");
            Register<DuelCommandProcessor>("duel");
            Register<KickCommandProcessor>("kick");
            Register<ArenaCommandProcessor>("arena");
            Register<CraftCommandProcessor>("craft");
            Register<JoinCommandProcessor>("join", "play");
            Register<LeaveCommandProcessor>("leave", "exit", "quit");

            Register<IslandInfoCommandProcessor>("island", "position", "where");
            Register<TrainingInfoCommandProcessor>("training");

            Register<TradeItemCommandProcessor>("sell", "buy");

            Register<ToggleCommandProcessor>("toggle");
            Register<TrainCommandProcessor>("train", "task", "strength", "attack", "mine", "defense", "mining", "wood", "crafting", "fishing", "fish", "woodcutting");
            Register<RavenfallCommandProcessor>("rpg", "raven", "ravenfall", "r");

            Register<ObserveCommandProcessor>("observe", "show");

            Register<SailCommandProcessor>("sail", "disembark");
            Register<TravelCommandProcessor>("travel");

            Register<DropEventCommandProcessor>("drop");
        }
    }
}
