using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerAndString : IBotRequest<string>
    {
        public User Player { get; }
        public string Value { get; }
        public PlayerAndString(User player, string value)
        {
            Player = player;
            Value = value;
        }
    }
}