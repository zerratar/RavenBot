using System.Collections.Generic;

namespace ROBot.Core.Chat.Commands
{
    public class Rest : Onsen
    {
        public override string Description => "Rest is an alias of Onsen (same command), this command is used for making your character rest to gain 2x more exp for the rested time. This only works on Heim and Kyo islands.";
        public override string UsageExample => "!rest exit";
        public override string Category => "Game";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("option", "Start resting or stop resting?", "exit")
        };
    }
}
