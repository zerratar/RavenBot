namespace ROBot.Core.Chat.Commands
{
    public class RaidWar : Raid
    {
        public override bool RequiresBroadcaster => true;
        public override string Description => "Start a raid war against another streamer: your players and theirs fight each other. Only the streamer can use it.";
        public override string UsageExample => "!raidwar zerratar";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "The target streamer you want to start a raid war with").Required()
        };
    }
}
