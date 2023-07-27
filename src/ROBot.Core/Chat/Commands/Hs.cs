namespace ROBot.Core.Chat.Commands
{
    public class Hs : Leaderboard
    {
        public override string Description => "Check how you are faring on the leaderboard. See https://www.ravenfall.stream/leaderboard";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("skill", "Which skill do you want to check? Leave empty for overall"),
        };
    }
}
