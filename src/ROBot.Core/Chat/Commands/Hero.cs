using RavenBot.Core.Handlers;
using System.Collections.Generic;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>
    /// !hero: join the fight beside the streamer's hero on Ravebridge; !hero heal, taunt and cheer help the hero, and
    /// !hero revive gets a downed hero back up. The game's !quest handles it (as !quest hero).
    /// </summary>
    public class Hero : Quest
    {
        public override string Category => "Events";
        public override string Description => "Hero mode on Ravebridge: join the fight beside the streamer's hero and help with heals, taunts, cheers, revives and boon votes, or send an elite enemy for coins. The streamer starts and ends hero mode with !hero start and !hero end.";
        public override string UsageExample => "!hero cheer";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "Leave it out (or type join) to join the fight. heal heals the hero, taunt pulls enemies off the hero, cheer gives the hero more damage for a while, revive votes to get a downed hero back up, 1, 2 or 3 votes for a boon between waves, and elite sends an elite enemy at the hero during a wave for 1500 coins (send it twice to pay). start and end are for the streamer only.",
                "join", "heal", "taunt", "cheer", "revive", "elite", "1", "2", "3", "start", "end").NotRequired()
        };
        protected override string ArgumentsFor(ICommand cmd) => ("hero " + (cmd.Arguments ?? string.Empty)).Trim();
    }
}
