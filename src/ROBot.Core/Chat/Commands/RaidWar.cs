namespace ROBot.Core.Chat.Commands
{
    public class RaidWar : Raid
    {
        public override bool RequiresBroadcaster => true;
        public override string Description => "This command allows you start a raid war against another streamer";
        public override string UsageExample => "!raidwar zerratar";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "The target streamer you want to start a raid war with").Required()
        };
    }
}
