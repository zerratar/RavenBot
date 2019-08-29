using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class ArenaKickRequest
    {
        public Player Player { get; }
        public Player TargetPlayer { get; }

        public ArenaKickRequest(Player player, Player targetPlayer)
        {
            Player = player;
            TargetPlayer = targetPlayer;
        }
    }
}