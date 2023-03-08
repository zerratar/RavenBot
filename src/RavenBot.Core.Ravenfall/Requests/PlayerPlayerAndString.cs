using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerPlayerAndString
    {
        public User Player { get; }
        public User Target { get; }
        public string Value { get; }
        public PlayerPlayerAndString(User player, User target, string value)
        {
            Player = player;
            Target = target;
            Value = value;
        }
    }
}