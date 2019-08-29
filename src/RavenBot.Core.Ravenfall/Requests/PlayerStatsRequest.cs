using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerStatsRequest
    {
        public Player Player { get; }
        public string Skill { get; }

        public PlayerStatsRequest(Player player, string skill)
        {
            Player = player;
            Skill = skill;
        }
    }
}