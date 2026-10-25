using RavenBot.Core.Handlers;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>!title: the titles a player has earned, and !title 2 or !title off to wear one or none (!quest title).</summary>
    public class Title : Quest
    {
        public override string Description => "The titles you have earned from achievements and quest chains; !title 2 wears one, !title off none.";
        public override string UsageExample => "!title 2";
        protected override string ArgumentsFor(ICommand cmd) => ("title " + (cmd.Arguments ?? string.Empty)).Trim();
    }

    public class Titles : Title
    {
    }
}
