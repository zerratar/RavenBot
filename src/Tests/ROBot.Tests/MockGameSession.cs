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
        public Player Owner { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Contains(string userId)
        {
            return true;
        }

        public bool ContainsUsername(string username)
        {
            return true;
        }

        public Player Get(string twitchId)
        {
            return new Player();
        }

        public Player Get(ICommandSender user)
        {
            return new Player();
        }

        public Player GetBroadcaster()
        {
            return new Player();
        }

        public Player GetUserByName(string username)
        {
            return new Player();
        }

        public Player Join(ICommandSender user, string identifier = "1")
        {
            return new Player();
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