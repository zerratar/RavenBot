using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

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
                    player.Platform = "unknown";
                    player.PlatformId = null;
                    users.Add(player);
                }

                // Re-read the name on every call, not only when the user is first seen.
                //
                // This list is process wide and the bot stays up for weeks, so a cached user used to
                // keep whatever name they had the first time we saw them. That is what made a Twitch
                // rename stick: the session owner kept the old name, every channel the bot joins
                // comes from that name, so the bot rejoined the old channel forever and only a
                // restart cleared it. The current name is already sitting in settings by the time we
                // get here, both from the session packet and from Ravenfall pushing settings when
                // the streamer logs in on the website.
                ApplySettings(player, settingsManager.Get(userId));

                return player;
            }
        }

        /// <summary>
        /// Copies the current name and account id from Ravenfall user settings onto a cached user.
        /// Safe to call repeatedly, and only writes values that are actually present so a user we
        /// have no settings for keeps whatever chat told us about them.
        /// </summary>
        private static void ApplySettings(User player, UserSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            // Ravenfall pushes a rename by rewriting the settings file, so the file is re-read
            // before anything is looked at. HasValues reads the dictionary directly and would
            // otherwise report empty for a user whose file has not been opened yet, which would skip
            // the whole update.
            settings.ReloadIfNecessary();

            if (!settings.HasValues)
            {
                return;
            }

            player.Settings = settings;

            var name = settings.RavenfallUserName;
            if (!string.IsNullOrEmpty(name) && !name.Equals(player.Username, StringComparison.Ordinal))
            {
                // A display name that came from the platform is better than the login name, so it is
                // only replaced when it was derived from the old login rather than set from chat.
                var displayNameWasDerived = string.IsNullOrEmpty(player.DisplayName)
                    || string.IsNullOrEmpty(player.Username)
                    || player.DisplayName.Equals(player.Username, StringComparison.OrdinalIgnoreCase);

                player.Username = name;

                if (displayNameWasDerived)
                {
                    player.DisplayName = name;
                }
            }

            // Only claim the account as the identity when nothing better is known. A user first seen
            // in chat already carries their real platform id, which must not be overwritten.
            if (string.IsNullOrEmpty(player.PlatformId) || player.Platform == "unknown")
            {
                player.PlatformId = player.Id.ToString();
                player.Platform = "ravenfall";
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
                    // Username is assigned as well as DisplayName. This is the path that matches on a
                    // platform id, which is the one thing a rename does not change, so it is exactly
                    // where a renamed user is found; leaving Username alone here meant the match
                    // succeeded and then kept the old name anyway.
                    if (!string.IsNullOrEmpty(username))
                    {
                        user.Username = username;
                        user.DisplayName = username;
                    }
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

        public void SetBroadcaster(string platformId, string username, string platform = "twitch")
        {
            lock (mutex)
            {
                var existing = GetByUserId(platformId, platform);
                if (existing != null)
                {
                    existing.IsBroadcaster = true;
                    return;
                }

                var user = Get(platformId, username, platform);
                user.IsBroadcaster = true;
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public User GetByUserId(string twitchUserId, string platform = "twitch")
        {
            return GetById(twitchUserId, platform);
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
            var settings = settingsManager.Get(platformId, platform);
            var user = new User(Guid.Empty, Guid.Empty, username, username, null, platform, platformId, false, false, false, false, false, false, "1");
            if (settings.HasValues)
            {
                user.Id = settings.RavenfallUserId;
                user.Platform = platform;
                user.IsGameAdministrator = settings.IsAdministrator;
                user.IsGameModerator = settings.IsModerator;

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