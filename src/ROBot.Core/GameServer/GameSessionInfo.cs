using RavenBot.Core.Ravenfall.Models;
using System;

namespace ROBot.Core.GameServer
{
    public class GameSessionInfo
    {
        public Guid UserId { get; set; }
        public Guid SessionId { get; set; }
        public DateTime Created { get; set; }
        public Player Owner { get; set; }
    }
}