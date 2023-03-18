using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System;
using System.Linq;

namespace RavenBot.Core.Ravenfall
{
    public class UserProvider : IUserProvider
    {

        private readonly System.Collections.Generic.List<User> users = new System.Collections.Generic.List<User>();

        private readonly object mutex = new object();
        private readonly IUserSettingsManager settingsManager;

        public UserProvider(IUserSettingsManager settingsManager)
        {
            this.settingsManager = settingsManager;
        }

        public int Count
        {
            get
            {
                lock (mutex)
                {
                    return users.Count;
                }
            }
        }

        public User Get(Guid userId)
        {
            lock (mutex)
            {
                var player = users.FirstOrDefault(x => x.Id == userId);
                if (player == null)
                {
                    player = new User();
                    player.Id = userId;

                    var settings = settingsManager.Get(userId);
                    if (settings.HasValues)
                    {
                        player.DisplayName = player.Username = settings.RavenfallUserName;
                        player.PlatformId = userId.ToString();
                        player.Platform = "ravenfall";
                        player.Settings = settings;
                    }
                    else
                    {
                        player.Platform = "unknown";
                        player.PlatformId = null;
                    }

                    users.Add(player);
                }

                return player;
            }
        }

        public User Get(ICommand cmd)
        {
            return Get(cmd.Sender);
        }

        public User Get(ICommandSender sender, string identifier = null)
        {
            lock (mutex)
            {
                if (string.IsNullOrEmpty(identifier?.Trim()))
                    identifier = "1";

                var user = users.FirstOrDefault(x => x.Username == sender.Username || x.PlatformId == sender.UserId && x.Identifier == identifier);
                if (user == null)
                {
                    user = new User();
                    users.Add(user);
                }

                user.Platform = sender.Platform;
                user.PlatformId = sender.UserId;
                user.Username = sender.Username;
                user.Platform = sender.Platform;
                user.DisplayName = sender.DisplayName;
                user.Color = sender.ColorHex;
                user.IsBroadcaster = sender.IsBroadcaster;
                user.IsModerator = sender.IsModerator;
                user.IsSubscriber = sender.IsSubscriber;
                user.IsVip = sender.IsVip;
                user.Identifier = identifier;

                LoadSettings(user);

                return user;
            }
        }

        public User Get(string userId, string username, string platform)
        {
            lock (mutex)
            {
                var user = users.FirstOrDefault(x => (x.Username == username || x.PlatformId == userId) && (x.Platform == null || x.Platform == platform));
                if (user != null)
                {
                    user.Platform = platform;
                    user.PlatformId = userId;
                    user.DisplayName = username;
                    return user;
                }
                user = CreateUser(username, userId, platform);
                user.PlatformId = userId;
                users.Add(user);
                return user;
            }
        }

        public User Get(string username)
        {
            lock (mutex)
            {
                if (string.IsNullOrEmpty(username)) return null;
                if (username.StartsWith("@")) username = username.Substring(1);
                var user = users.FirstOrDefault(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (user != null) return user;
                user = CreateUser(username, null, null);
                users.Add(user);
                return user;
            }
        }

        public User Get(string username, string platform)
        {

            lock (mutex)
            {
                if (string.IsNullOrEmpty(username)) return null;
                if (username.StartsWith("@")) username = username.Substring(1);
                var user = users.FirstOrDefault(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)
                && x.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));
                if (user != null) return user;

                user = CreateUser(username, null, platform);

                users.Add(user);
                return user;
            }
        }

        public User GetByUserId(string twitchUserId, string platform = "twitch")
        {
            lock (mutex)
            {
                return users.FirstOrDefault(x => x.PlatformId == twitchUserId && x.Platform == platform);
            }
        }


        public bool Contains(User player)
        {
            lock (mutex)
            {
                if (users.Contains(player))
                    return true;

                return GetById(player.PlatformId) != null; ;
            }
        }

        public User GetById(string userId, string platform = "twitch")
        {
            lock (mutex)
            {
                return users.FirstOrDefault(x => x.PlatformId == userId && x.Platform == platform);
            }
        }

        public bool RemoveById(string twitchId, string platform = "twitch")
        {
            lock (mutex)
            {
                var user = GetById(twitchId, platform);
                if (user == null) return false;
                return users.Remove(user);
            }
        }

        public User GetBroadcaster()
        {
            lock (mutex)
            {
                return users.FirstOrDefault(x => x.IsBroadcaster);
            }
        }

        private void LoadSettings(User user)
        {
            var settings = user.Id != Guid.Empty
                ? settingsManager.Get(user.Id)
                : settingsManager.Get(user.PlatformId, user.Platform);

            if (settings.HasValues)
            {
                user.Settings = settings;
                user.Id = settings.RavenfallUserId;
            }
        }

        private User CreateUser(string username, string platformId, string platform)
        {
            var user = new User(Guid.Empty, Guid.Empty, username, username, null, platform, platformId, false, false, false, false, "1");
            var settings = settingsManager.Get(platformId, platform);
            if (settings.HasValues)
            {
                user.Id = settings.RavenfallUserId;
                user.Platform = platform;

                switch (platform)
                {
                    case "twitch":
                        user.PlatformId = settings.TwitchUserId;
                        break;
                    case "youtube":
                        user.PlatformId = settings.YouTubeUserId;
                        break;
                    case "discord":
                        user.PlatformId = settings.DiscordUserId;
                        break;
                    case "kick":
                        user.PlatformId = settings.KickUserId;
                        break;
                }
                user.Settings = settings;
            }

            return user;
        }

    }
}