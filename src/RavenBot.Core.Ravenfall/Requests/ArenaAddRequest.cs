using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class ArenaAddRequest
    {
        public User Player { get; }
        public User TargetPlayer { get; }

        public ArenaAddRequest(User player, User targetPlayer)
        {
            Player = player;
            TargetPlayer = targetPlayer;
        }
    }
}