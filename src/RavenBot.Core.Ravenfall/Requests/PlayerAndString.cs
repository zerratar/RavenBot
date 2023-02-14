using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerAndString : IBotRequest<string>
    {
        public Player Player { get; }
        public string Value { get; }
        public PlayerAndString(Player player, string value)
        {
            Player = player;
            Value = value;
        }
    }
}