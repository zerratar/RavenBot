using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System;
using System.Linq;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class PlayerProvider : IPlayerProvider
    {

        private readonly System.Collections.Generic.List<Player> createdPlayers = new System.Collections.Generic.List<Player>();

        private readonly object mutex = new object();
        private readonly IUserSettingsManager settingsManager;

        public PlayerProvider(IUserSettingsManager settingsManager)
        {
            this.settingsManager = settingsManager;
        }

        public int Count
        {
            get
            {
                lock (mutex)
                {
                    return createdPlayers.Count;
                }
            }
        }

        public Player Get(Guid userId)
        {
            lock (mutex)
            {
                var player = createdPlayers.FirstOrDefault(x => x.Id == userId);
                if (player == null)
                {
                    player = new Player();
                    player.Id = userId;

                    var settings = settingsManager.Get(userId);
                    if (settings.HasValues)
                    {
                        player.DisplayName = player.Username = settings.RavenfallUserName;
                        player.PlatformId = userId.ToString();
                        player.Platform = "ravenfall";
                        player.Settings = settings;
                    }

                    createdPlayers.Add(player);
                }

                return player;
            }
        }

        public Player Get(ICommandSender sender, string identifier = null)
        {
            lock (mutex)
            {
                if (string.IsNullOrEmpty(identifier?.Trim()))
                    identifier = "1";

                var player = createdPlayers.FirstOrDefault(x => x.Username == sender.Username || x.PlatformId == sender.UserId && x.Identifier == identifier);
                if (player == null)
                {
                    player = new Player();
                    createdPlayers.Add(player);
                }

                player.Platform = sender.Platform;
                player.PlatformId = sender.UserId;
                player.Username = sender.Username;
                player.Platform = sender.Platform;
                player.DisplayName = sender.DisplayName;
                player.Color = sender.ColorHex;
                player.IsBroadcaster = sender.IsBroadcaster;
                player.IsModerator = sender.IsModerator;
                player.IsSubscriber = sender.IsSubscriber;
                player.IsVip = sender.IsVip;
                player.Identifier = identifier;

                LoadSettings(player);

                return player;
            }
        }

        public Player Get(string userId, string username, string platform = "twitch")
        {
            lock (mutex)
            {
                var player = createdPlayers.FirstOrDefault(x => (x.Username == username || x.PlatformId == userId) && (x.Platform == null || x.Platform == platform));
                if (player != null)
                {
                    player.Platform = platform;
                    player.PlatformId = userId;
                    player.DisplayName = username;
                    return player;
                }
                player = CreatePlayer(username, platform);
                player.PlatformId = userId;
                createdPlayers.Add(player);
                return player;
            }
        }

        public Player Get(string username, string platform = "twitch")
        {

            lock (mutex)
            {
                if (string.IsNullOrEmpty(username)) return null;
                if (username.StartsWith("@")) username = username.Substring(1);
                var player = createdPlayers.FirstOrDefault(x => x.Username == username);
                if (player != null) return player;

                player = CreatePlayer(username, platform);

                createdPlayers.Add(player);
                return player;
            }
        }

        public Player GetByUserId(string twitchUserId, string platform = "twitch")
        {
            lock (mutex)
            {
                return createdPlayers.FirstOrDefault(x => x.PlatformId == twitchUserId && x.Platform == platform);
            }
        }


        public bool Contains(Player player)
        {
            lock (mutex)
            {
                if (createdPlayers.Contains(player))
                    return true;

                return GetById(player.PlatformId) != null; ;
            }
        }

        public Player GetById(string userId, string platform = "twitch")
        {
            lock (mutex)
            {
                return createdPlayers.FirstOrDefault(x => x.PlatformId == userId && x.Platform == platform);
            }
        }

        public bool RemoveById(string twitchId, string platform = "twitch")
        {
            lock (mutex)
            {
                var user = GetById(twitchId, platform);
                if (user == null) return false;
                return createdPlayers.Remove(user);
            }
        }

        public Player GetBroadcaster()
        {
            lock (mutex)
            {
                return createdPlayers.FirstOrDefault(x => x.IsBroadcaster);
            }
        }

        private void LoadSettings(Player player)
        {
            var settings = player.Id != Guid.Empty ? settingsManager.Get(player.Id) : settingsManager.Get(player.PlatformId, player.Platform);
            if (settings.HasValues)
            {
                player.Settings = settings;
                player.Id = settings.RavenfallUserId;
            }
        }

        private Player CreatePlayer(string username, string platform)
        {
            Player player = new Player(Guid.Empty, null, username, username, null, platform, false, false, false, false, "1");
            var settings = settingsManager.Get(username, platform);
            if (settings.HasValues)
            {
                player.Id = settings.RavenfallUserId;
                player.Platform = platform;

                switch (platform)
                {
                    case "twitch":
                        player.PlatformId = settings.TwitchUserId;
                        break;
                    case "youtube":
                        player.PlatformId = settings.YouTubeUserId;
                        break;
                    case "discord":
                        player.PlatformId = settings.DiscordUserId;
                        break;
                    case "kick":
                        player.PlatformId = settings.KickUserId;
                        break;
                }
                player.Settings = settings;
            }

            return player;
        }

    }
}