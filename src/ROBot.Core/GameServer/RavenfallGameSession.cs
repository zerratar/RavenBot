using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Ravenfall.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ROBot.Core.GameServer
{
    public class RavenfallGameSession : IGameSession
    {
        private readonly PlayerProvider playerProvider;
        private readonly IBotServer server;
        public RavenfallGameSession(
            IBotServer server,
            Guid id,
            string userId,
            string name,
            DateTime created)
        {
            this.playerProvider = new PlayerProvider();
            this.server = server;
            this.Id = id;
            this.UserId = userId;
            this.Name = name;
            this.Created = created;
        }

        public Guid Id { get; }

        // mutable in case user changes name during active session
        public string Name { get; set; }
        public string UserId { get; }
        public DateTime Created { get; }

        public int UserCount => playerProvider.Count;

        public Player Join(ICommandSender sender, string identifier = "1")
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

        public void SendChatMessage(string username, string message)
        {
            var user = GetUserByName(username);
            if (user == null) return;
            //connection.Send(new BotPlayerMessage
            //{
            //    Session = Name,
            //    Username = user.Username,
            //    Message = message
            //}, Shinobytes.Ravenfall.RavenNet.SendOption.Reliable);
        }

        public Player GetUserByName(string username)
        {
            return playerProvider.Get(username);
        }

        public Player Get(ICommandSender user)
        {
            return playerProvider.Get(user);
        }

        public Player Get(string userId)
        {
            return playerProvider.GetById(userId);
        }

        public Player GetBroadcaster()
        {
            var broadcaster = playerProvider.GetById(UserId);
            if (broadcaster == null)
            {
                broadcaster = playerProvider.GetBroadcaster();
            }

            if (broadcaster == null)
            {
                broadcaster = playerProvider.Get(UserId, Name);
                broadcaster.IsBroadcaster = true;
            }

            return broadcaster;
        }

        public bool ContainsUsername(string username)
        {
            return playerProvider.Get(username) != null;
        }

        public bool Contains(string twitchId)
        {
            return Get(twitchId) != null;
        }

        public bool Contains(Player user)
        {
            return playerProvider.Contains(user);
        }

        public bool RemoveUser(string twitchId)
        {
            return playerProvider.RemoveById(twitchId);
        }

    }
}
