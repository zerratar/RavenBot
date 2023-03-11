using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Ravenfall.Models;
using System;

namespace ROBot.Core
{
    public interface IGameSession
    {
        Guid Id { get; }
        Guid RavenfallUserId { get; set; }
        ICommandChannel Channel { get; set; }
        User Owner { get; set; }
        string Name { get; set; }
        public DateTime Created { get; }
        int UserCount { get; }
        User GetBroadcaster();
        User Get(string twitchId);
        User GetUserByName(string username);
        User Get(ICommandSender user);
        User Join(ICommandSender user, string identifier = "1");
        User Get(ICommand cmd);
        bool Contains(string userId);
        bool ContainsUsername(string username);
        void Leave(string userId);
    }
}
