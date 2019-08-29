using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class BuyItemRequest
    {
        public BuyItemRequest(
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