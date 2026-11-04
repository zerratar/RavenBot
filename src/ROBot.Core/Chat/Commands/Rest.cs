using System.Collections.Generic;

namespace ROBot.Core.Chat.Commands
{
    public class Rest : Onsen
    {
        // the description and inputs are !onsen's, so the website shows !rest as its other name
        public override string UsageExample => "!rest leave";
        public override string Category => "Game";
    }
}
