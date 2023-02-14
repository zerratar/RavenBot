using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerPlayerAndString
    {
        public Player Player { get; }
        public Player Target { get; }
        public string Value { get; }
        public PlayerPlayerAndString(Player player, Player target, string value)
        {
            Player = player;
            Target = target;
            Value = value;
        }
    }
}