using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class ArenaAddRequest
    {
        public Player Player { get; }
        public Player TargetPlayer { get; }

        public ArenaAddRequest(Player player, Player targetPlayer)
        {
            Player = player;
            TargetPlayer = targetPlayer;
        }
    }
}