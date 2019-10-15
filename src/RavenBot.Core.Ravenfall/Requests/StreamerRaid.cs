using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class StreamerRaid
    {
        public StreamerRaid(
            Player player,
            bool war)
        {
            Player = player;
            War = war;
        }

        public Player Player { get; }
        public bool War { get; }
    }
}