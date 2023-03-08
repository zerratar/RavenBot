using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class SetScaleRequest
    {
        public User Player { get; }
        public float Scale { get; }

        public SetScaleRequest(User player, float scale)
        {
            Player = player;
            Scale = scale;
        }
    }
}