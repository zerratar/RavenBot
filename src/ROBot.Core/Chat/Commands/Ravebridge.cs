using RavenBot.Core.Handlers;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>
    /// !ravebridge: join the Ravebridge event (the streamer opens it), !ravebridge leave to go back, !ravebridge
    /// follow or heal to help the streamer's hero, and start or end for the streamer and the moderators. The game's
    /// !quest handles it (as !quest ravebridge).
    /// </summary>
    public class Ravebridge : Quest
    {
        public override string Category => "Events";
        public override string Description => "Ravebridge, the event island far to the west: join it, leave it, or help the streamer's hero (follow, heal); the streamer starts and ends it.";
        public override string UsageExample => "!ravebridge";
        protected override string ArgumentsFor(ICommand cmd) => ("ravebridge " + (cmd.Arguments ?? string.Empty)).Trim();
    }

    public class Rb : Ravebridge
    {
    }
}
