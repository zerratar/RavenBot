using System;

namespace ROBot.Core.GameServer
{
    public class GameSessionInfo
    {
        public string TwitchUserId { get; set; }
        public string TwitchUserName { get; set; }
        public Guid SessionId { get; set; }
        public DateTime Created { get; set; }
    }

}