using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerStringRequest : IBotRequest<string>
    {
        public Player Player { get; }
        public string Value { get; }
        public PlayerStringRequest(Player player, string value)
        {
            Player = player;
            Value = value;
        }
    }
}