namespace ROBot.Core.Chat.Commands
{
    public class Req : Requirement
    {
        public override string Category => "Items";
        public override string Description => "Check what the crafting requirements are for a target item.";
        public override string UsageExample => "!req rune 2h sword";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "Which item do you want to check?"),
        };
    }
}
