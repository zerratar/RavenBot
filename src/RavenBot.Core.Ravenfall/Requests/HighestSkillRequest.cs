using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class HighestSkillRequest
    {
        public HighestSkillRequest(User player, string skill)
        {
            Player = player;
            Skill = skill;
        }

        public User Player { get; }
        public string Skill { get; }
    }
}