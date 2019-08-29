using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class SellItemRequest
    {
        public SellItemRequest(
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