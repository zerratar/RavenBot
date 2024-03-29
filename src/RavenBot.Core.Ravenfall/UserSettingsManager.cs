﻿using RavenBot.Core.Ravenfall.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RavenBot.Core.Ravenfall
{
    public class UserSettingsManager : IUserSettingsManager
    {
        private readonly string SettingsDirectory;
        private readonly ConcurrentDictionary<Guid, UserSettings> dict = new();
        private readonly ConcurrentDictionary<string, Guid> idLookup = new();

        public UserSettingsManager(string settingsDirectory = "../user-settings/")
        {
            this.SettingsDirectory = settingsDirectory;
            if (!System.IO.Directory.Exists(settingsDirectory))
            {
                System.IO.Directory.CreateDirectory(settingsDirectory);
            }

            var settingsFiles = System.IO.Directory.GetFiles(settingsDirectory, "*.json");
            foreach (var file in settingsFiles)
            {
                try
                {
                    LoadSettingsFile(file);
                }
                catch (Exception exc)
                {
                    System.IO.File.WriteAllText(file + ".error", exc.ToString());
                }
            }
        }

        private void LoadSettingsFile(string file)
        {
            var accId = System.IO.Path.GetFileNameWithoutExtension(file);
            // no longer supported.
            if (!Guid.TryParse(accId, out var userId))
            {
                System.IO.File.Delete(file);
            }
            else
            {
                var content = System.IO.File.ReadAllText(file);
                var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                var settings = new UserSettings(file, values);
                dict[userId] = settings;

                if (!string.IsNullOrEmpty(settings.DiscordUserId))
                    idLookup["discord_" + settings.DiscordUserId.ToLower()] = settings.RavenfallUserId;

                if (!string.IsNullOrEmpty(settings.TwitchUserId))
                    idLookup["twitch_" + settings.TwitchUserId.ToLower()] = settings.RavenfallUserId;

                if (!string.IsNullOrEmpty(settings.YouTubeUserId))
                    idLookup["youtube_" + settings.YouTubeUserId.ToLower()] = settings.RavenfallUserId;

                if (!string.IsNullOrEmpty(settings.KickUserId))
                    idLookup["kick_" + settings.KickUserId.ToLower()] = settings.RavenfallUserId;
            }
        }

        public UserSettings Get(string platformId, string platform)
        {
            if (string.IsNullOrEmpty(platformId) || string.IsNullOrEmpty(platform))
            {
                return UserSettings.Empty;
            }

            var id = ResolveAccountId(platformId, platform);
            if (id != Guid.Empty)
            {
                return Get(id);
            }

            return UserSettings.Empty;
        }

        public Guid ResolveAccountId(string platformId, string platform)
        {
            var key = (platform + "_" + platformId).ToLower();
            if (idLookup.TryGetValue(key, out var id))
            {
                return id;
            }

            // if null, check if there are new settings files to load in.
            var settingsFiles = System.IO.Directory.GetFiles(SettingsDirectory, "*.json");
            foreach (var file in settingsFiles)
            {
                var accId = System.IO.Path.GetFileNameWithoutExtension(file);
                if (!Guid.TryParse(accId, out var uid) || dict.ContainsKey(uid))
                    continue;

                LoadSettingsFile(file);
            }

            foreach (var settings in dict.Values.ToList())
            {
                if (settings.TryGet<string>(platform.ToLower() + "_id", out var i) && i.Equals(platformId, StringComparison.OrdinalIgnoreCase))
                {
                    return idLookup[key] = settings.RavenfallUserId;
                }
            }

            return Guid.Empty;
        }

        private string GetFile(Guid userId)
        {
            return System.IO.Path.Combine(SettingsDirectory, userId + ".json");
        }

        public UserSettings Get(Guid userId)
        {
            if (!dict.TryGetValue(userId, out var settings))
            {
                settings = new UserSettings(GetFile(userId));
                dict[userId] = settings;
            }

            return settings;
        }

        public IReadOnlyList<UserSettings> GetAll()
        {
            return dict.Values.ToList();
        }

        public T Get<T>(Guid userId, string key)
        {
            return Get(userId).Get<T>(key);
        }

        public T Get<T>(Guid userId, string key, T defaultValue)
        {
            return Get(userId).Get(key, defaultValue);
        }

        public bool TryGet<T>(Guid userId, string key, out T value)
        {
            return Get(userId).TryGet(key, out value);
        }

        public void Set<T>(Guid userId, string key, T value)
        {
            Get(userId).Set(key, value);
        }

        public void Set(Guid userId, Dictionary<string, object> userSettings)
        {
            var settings = Get(userId);
            foreach (var s in userSettings)
            {
                settings.Set(s.Key, s.Value);
            }
        }

    }
}