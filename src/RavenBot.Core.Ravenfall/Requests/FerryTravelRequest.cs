using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class FerryTravelRequest
    {
        public User Player { get; }
        public string Destination { get; }

        public FerryTravelRequest(User player, string destination)
        {
            Player = player;
            Destination = destination;
        }
    }
}