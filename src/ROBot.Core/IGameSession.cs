using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System;

namespace ROBot.Core
{
    public interface IGameSession
    {
        Guid Id { get; }
        Guid RavenfallUserId { get; set; }
        User Owner { get; set; }
        string Name { get; set; }
        public DateTime Created { get; }
        User GetBroadcaster();
        User Get(string twitchId);
        User Get(ICommandSender user);
        User GetUserByName(string username);
        User Join(ICommandSender user, string identifier = "1");
        int UserCount { get; }
        bool Contains(string userId);
        bool ContainsUsername(string username);
        void Leave(string userId);
    }
}
