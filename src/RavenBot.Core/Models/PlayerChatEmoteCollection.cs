using System.Collections.Generic;
using System.Linq;

namespace RavenBot.Core.Ravenfall.Models
{
    public class PlayerChatEmoteCollection
    {
        public PlayerChatEmoteCollection(User player, IEnumerable<string> emoteUrls)
        {
            Player = player;
            EmoteUrls = emoteUrls.ToList();
        }

        public User Player { get; }
        public IReadOnlyList<string> EmoteUrls { get; }
    }
}