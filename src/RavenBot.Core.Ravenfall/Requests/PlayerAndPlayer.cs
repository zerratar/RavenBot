using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerAndPlayer
    {
        public User Player { get; }
        public User TargetPlayer { get; }

        public PlayerAndPlayer(User player, User targetPlayer)
        {
            Player = player;
            TargetPlayer = targetPlayer;
        }
    }
}