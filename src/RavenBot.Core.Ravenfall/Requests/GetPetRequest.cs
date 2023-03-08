using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class GetPetRequest
    {
        public User Player { get; }

        public GetPetRequest(User player)
        {
            Player = player;
        }
    }
}