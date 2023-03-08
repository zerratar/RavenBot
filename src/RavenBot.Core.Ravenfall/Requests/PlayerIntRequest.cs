using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerIntRequest : IBotRequest<int>
    {
        public User Player { get; }
        public int Value { get; }
        public PlayerIntRequest(User player, int value)
        {
            Player = player;
            Value = value;
        }
    }
}