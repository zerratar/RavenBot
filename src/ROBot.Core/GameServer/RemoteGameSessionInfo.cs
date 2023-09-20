using RavenBot.Core.Ravenfall.Models;
using System;
using System.Collections.Generic;

namespace ROBot.Core.GameServer
{
    public class RemoteGameSessionInfo
    {
        public Guid UserId { get; set; }
        public Guid SessionId { get; set; }
        public DateTime Created { get; set; }
        public User Owner { get; set; }
        public Dictionary<string, object> Settings { get; set; }
    }
}