using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{

    public class ItemQueryRequest
    {
        public ItemQueryRequest(
            User player,
            string itemQuery)
        {
            Player = player;
            ItemQuery = itemQuery;
        }

        public User Player { get; }
        public string ItemQuery { get; }
    }
}