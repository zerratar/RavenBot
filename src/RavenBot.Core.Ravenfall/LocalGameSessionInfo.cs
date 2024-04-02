using System;
using System.Collections.Generic;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public class LocalGameSessionInfo
    {
        public Guid SessionId { get; set; }
        public DateTime SessionStart { get; set; }
        public User Owner { get; set; }
        public Dictionary<string, object> Settings { get; set; }
    }

    //public class BroadcastMessage
    //{
    //    public string User { get; set; }
    //    public string Message { get; set; }
    //}
}