using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class GetPetRequest
    {
        public Player Player { get; }

        public GetPetRequest(Player player)
        {
            Player = player;
        }
    }
}