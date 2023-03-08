using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class SetPetRequest
    {
        public User Player { get; }
        public string Pet { get; }

        public SetPetRequest(User player, string pet)
        {
            Player = player;
            Pet = pet;
        }
    }
}