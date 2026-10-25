using RavenBot.Core.Handlers;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>
    /// !hero: join the fight beside the streamer's hero on Ravebridge; !hero heal, taunt and cheer help the hero, and
    /// !hero revive gets a downed hero back up. The game's !quest handles it (as !quest hero).
    /// </summary>
    public class Hero : Quest
    {
        public override string Category => "Events";
        public override string Description => "Hero mode on Ravebridge: join the fight beside the streamer's hero, and help with heal, taunt, cheer, or revive when the hero is down.";
        public override string UsageExample => "!hero cheer";
        protected override string ArgumentsFor(ICommand cmd) => ("hero " + (cmd.Arguments ?? string.Empty)).Trim();
    }
}
