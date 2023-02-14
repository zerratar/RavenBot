using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerAndPlayer
    {
        public Player Player { get; }
        public Player TargetPlayer { get; }

        public PlayerAndPlayer(Player player, Player targetPlayer)
        {
            Player = player;
            TargetPlayer = targetPlayer;
        }
    }
}