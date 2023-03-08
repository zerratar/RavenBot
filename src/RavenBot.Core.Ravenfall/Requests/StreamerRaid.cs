using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class StreamerRaid
    {
        public StreamerRaid(
            User player,
            bool war)
        {
            Player = player;
            War = war;
        }

        public User Player { get; }
        public bool War { get; }
    }
}