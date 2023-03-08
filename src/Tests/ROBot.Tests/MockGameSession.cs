using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core;
using System;

namespace ROBot.Tests
{
    internal class MockGameSession : IGameSession
    {
        public MockGameSession(string session)
        {
            Id = Guid.NewGuid();
            this.Name = session;
        }
        public Guid Id { get; }
        public string Name { get; set; }
        public string UserId { get; set; }

        public DateTime Created => DateTime.UtcNow;

        public int UserCount => 0;

        public Guid RavenfallUserId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public User Owner { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Contains(string userId)
        {
            return true;
        }

        public bool ContainsUsername(string username)
        {
            return true;
        }

        public User Get(string twitchId)
        {
            return new User();
        }

        public User Get(ICommandSender user)
        {
            return new User();
        }

        public User GetBroadcaster()
        {
            return new User();
        }

        public User GetUserByName(string username)
        {
            return new User();
        }

        public User Join(ICommandSender user, string identifier = "1")
        {
            return new User();
        }

        public void Leave(string userId)
        {
        }

        public void SendChatMessage(string username, string message)
        {
            Console.WriteLine(username + ": " + message);
        }
    }
}