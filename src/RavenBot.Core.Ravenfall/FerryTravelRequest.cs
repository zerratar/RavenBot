using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public class FerryTravelRequest
    {
        public Player Player { get; }
        public string Destination { get; }

        public FerryTravelRequest(Player player, string destination)
        {
            this.Player = player;
            this.Destination = destination;
        }
    }
}