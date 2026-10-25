using RavenBot.Core.Handlers;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>!achievements: the achievements a player has reached and the closest next ones (!quest achievements).</summary>
    public class Achievements : Quest
    {
        public override string Description => "Your achievements: the medals reached and the closest next ones.";
        public override string UsageExample => "!achievements";
        protected override string ArgumentsFor(ICommand cmd) => "achievements";
    }

    public class Achievement : Achievements
    {
    }
}
