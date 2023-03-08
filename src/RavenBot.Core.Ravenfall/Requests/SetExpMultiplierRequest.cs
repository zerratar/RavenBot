using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class SetExpMultiplierRequest
    {
        public SetExpMultiplierRequest(User player, int expMultiplier)
        {
            this.Player = player;
            this.ExpMultiplier = expMultiplier;
        }

        public int ExpMultiplier { get; }
        public User Player { get; }
    }
}