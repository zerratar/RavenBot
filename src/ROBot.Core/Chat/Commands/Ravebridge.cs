using RavenBot.Core.Handlers;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>
    /// !ravebridge: join the Ravebridge event (the streamer opens it), !ravebridge leave to go back, !ravebridge
    /// follow or heal to help the streamer's hero, revive to get a downed hero back up, and start or end for the
    /// streamer and the moderators. The game's !quest handles it (as !quest ravebridge).
    /// </summary>
    public class Ravebridge : Quest
    {
        public override string Category => "Events";
        public override string Description => "Ravebridge, the event island far to the west: join it, leave it, or help the streamer's hero (follow, heal, revive when the hero is down); the streamer starts and ends it.";
        public override string UsageExample => "!ravebridge heal";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action",
                "Leave empty to join Ravebridge while it is open. " +
                "leave takes you back home. " +
                "follow (or fight) has you fight beside the streamer's hero, heal has you heal the hero, stop stops helping. " +
                "revive (or res) votes to get a downed hero back up. " +
                "status tells whether it is open and how many are there. " +
                "start and end open and close it, for the streamer and moderators only.",
                "leave", "follow", "heal", "stop", "revive", "status", "start", "end").NotRequired(),
        };
        protected override string ArgumentsFor(ICommand cmd) => ("ravebridge " + (cmd.Arguments ?? string.Empty)).Trim();
    }

    public class Rb : Ravebridge
    {
    }
}
