using System.Collections.Generic;
using System.Linq;

namespace RavenBot.Core.Ravenfall.Models
{
    public class PlayerChatEmoteCollection
    {
        public PlayerChatEmoteCollection(Player player, IEnumerable<string> emoteUrls)
        {
            Player = player;
            EmoteUrls = emoteUrls.ToList();
        }

        public Player Player { get; }
        public IReadOnlyList<string> EmoteUrls { get; }
    }
}