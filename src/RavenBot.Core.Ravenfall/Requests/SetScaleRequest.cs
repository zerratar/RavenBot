using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class SetScaleRequest
    {
        public Player Player { get; }
        public float Scale { get; }

        public SetScaleRequest(Player player, float scale)
        {
            Player = player;
            Scale = scale;
        }
    }
}