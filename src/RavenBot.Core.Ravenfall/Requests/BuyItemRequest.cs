using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class ItemQueryRequest
    {
        public ItemQueryRequest(
            Player player,
            string itemQuery)
        {
            Player = player;
            ItemQuery = itemQuery;
        }

        public Player Player { get; }
        public string ItemQuery { get; }
    }
}