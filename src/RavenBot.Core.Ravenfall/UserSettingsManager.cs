using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class UserSettingsManager : IUserSettingsManager
    {
        const string SettingsDirectory = "../user-settings/";

        private readonly ConcurrentDictionary<string, UserSettings> dict
            = new ConcurrentDictionary<string, UserSettings>();
        public UserSettingsManager()
        {
            if (!System.IO.Directory.Exists(SettingsDirectory))
            {
                System.IO.Directory.CreateDirectory(SettingsDirectory);
            }

            var settingsFiles = System.IO.Directory.GetFiles(SettingsDirectory, "*.json");
            foreach (var file in settingsFiles)
            {
                try
                {
                    var userId = System.IO.Path.GetFileNameWithoutExtension(file);
                    var content = System.IO.File.ReadAllText(file);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    dict[userId] = new UserSettings(file, settings);
                }
                catch (System.Exception exc)
                {
                    System.IO.File.WriteAllText(file + ".error", exc.ToString());
                }
            }
        }

        private string GetFile(string userId)
        {
            return System.IO.Path.Combine(SettingsDirectory, userId + ".json");
        }

        public UserSettings Get(string userId)
        {
            if (!dict.TryGetValue(userId, out var settings))
            {
                settings = new UserSettings(GetFile(userId));
                dict[userId] = settings;
            }

            return settings;
        }

        public T Get<T>(string userId, string key)
        {
            return Get(userId).Get<T>(key);
        }

        public T Get<T>(string userId, string key, T defaultValue)
        {
            return Get(userId).Get<T>(key, defaultValue);
        }

        public bool TryGet<T>(string userId, string key, out T value)
        {
            return Get(userId).TryGet<T>(key, out value);
        }

        public void Set<T>(string userId, string key, T value)
        {
            Get(userId).Set(key, value);
        }
    }
}