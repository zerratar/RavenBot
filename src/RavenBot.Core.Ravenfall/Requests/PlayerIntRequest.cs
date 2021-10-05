using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerIntRequest : IBotRequest<int>
    {
        public Player Player { get; }
        public int Value { get; }
        public PlayerIntRequest(Player player, int value)
        {
            Player = player;
            Value = value;
        }
    }
}