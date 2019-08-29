using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class HighestSkillRequest
    {
        public HighestSkillRequest(Player player, string skill)
        {
            Player = player;
            Skill = skill;
        }

        public Player Player { get; }
        public string Skill { get; }
    }
}