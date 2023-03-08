using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class CraftRequest
    {
        public User Player { get; }
        public string Category { get; }
        public string Type { get; }

        public CraftRequest(User player, string category, string type)
        {
            Player = player;
            Category = category;
            Type = type;
        }
    }
}