using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Models;
using System;

namespace ROBot.Core.GameServer
{
    public class RavenfallGameSession : IGameSession
    {
        private readonly UserProvider playerProvider;
        private readonly IBotServer server;

        public RavenfallGameSession(
            IBotServer server,
            IUserSettingsManager userSettingsManager,
            Guid id,
            Guid ravenfallUserId,
            User owner,
            DateTime created)
        {
            this.playerProvider = new UserProvider(userSettingsManager);
            this.server = server;
            this.Id = id;
            this.RavenfallUserId = ravenfallUserId;
            this.Owner = owner;
            this.Name = owner.Username;
            this.Created = created;
        }

        public Guid Id { get; }
        public Guid RavenfallUserId { get; set; }
        public User Owner { get; set; }
        // mutable in case user changes name during active session
        public string Name { get; set; }
        public DateTime Created { get; }

        public int UserCount => playerProvider.Count;

        public ICommandChannel Channel { get; set; }

        public User Join(ICommandSender sender, string identifier = "1")
        {
            var user = playerProvider.Get(sender, identifier);
            if (user == null) return null;
            return user;
            //connection.Send(new BotPlayerJoin
            //{
            //    Session = Name,
            //    Username = username,
            //    TwitchId = twitchId,
            //    YouTubeId = youtubeId
            //}, Shinobytes.Ravenfall.RavenNet.SendOption.Reliable);
        }

        public void Leave(string twitchUserId)
        {
            //var user = Get(twitchUserId);
            //if (user == null) return;
            playerProvider.RemoveById(twitchUserId);
            //connection.Send(new BotPlayerLeave
            //{
            //    Session = Name,
            //    Username = user.Username
            //}, Shinobytes.Ravenfall.RavenNet.SendOption.Reliable);
        }

        public User GetUserByName(string username)
        {
            return playerProvider.Get(username);
        }

        public User GetUserByName(string username, string platform)
        {
            return playerProvider.Get(username, platform);
        }

        public User Get(ICommandSender user)
        {
            return playerProvider.Get(user);
        }

        public User Get(ICommand cmd)
        {
            return playerProvider.Get(cmd.Sender);
        }


        public User Get(string userId)
        {
            return playerProvider.GetById(userId);
        }

        public User GetBroadcaster()
        {
            if (Owner != null)
            {
                return Owner;
            }

            return playerProvider.GetBroadcaster();
        }

        public bool ContainsUsername(string username)
        {
            return playerProvider.Get(username) != null;
        }

        public bool Contains(string twitchId)
        {
            return Get(twitchId) != null;
        }

        public bool Contains(User user)
        {
            return playerProvider.Contains(user);
        }

        public bool RemoveUser(string twitchId)
        {
            return playerProvider.RemoveById(twitchId);
        }

    }
}
