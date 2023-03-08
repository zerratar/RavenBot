using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class DuelPlayerRequest
    {
        public DuelPlayerRequest(User playerA, User playerB)
        {
            this.playerA = playerA;
            this.playerB = playerB;
        }

        public User playerA { get; }
        public User playerB { get; }
    }
}