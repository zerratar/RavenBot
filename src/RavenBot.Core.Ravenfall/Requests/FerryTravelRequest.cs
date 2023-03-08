using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public class FerryTravelRequest
    {
        public User Player { get; }
        public string Destination { get; }

        public FerryTravelRequest(User player, string destination)
        {
            this.Player = player;
            this.Destination = destination;
        }
    }
}