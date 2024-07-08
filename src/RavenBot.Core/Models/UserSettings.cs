using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RavenBot.Core.Ravenfall.Models
{
    public class UserSettings
    {
        public static readonly UserSettings Empty = new UserSettings();

        private readonly string file;
        private readonly object ioMutex = new object();

        private DateTime loadedTime;
        private ConcurrentDictionary<string, object> dict;

        public UserSettings()
        {
            dict = new ConcurrentDictionary<string, object>();
        }

        public UserSettings(string file, Dictionary<string, object> src)
        {
            this.file = file;
            loadedTime = DateTime.UtcNow;
            if (src != null)
            {
                dict = new ConcurrentDictionary<string, object>(src);
            }
            else
            {
                dict = new ConcurrentDictionary<string, object>();
            }
        }

        public bool HasValues => dict.Count > 0;

        public UserSettings(string file)
        {
            this.file = file;
            loadedTime = DateTime.UtcNow;
            dict = new ConcurrentDictionary<string, object>();
        }

        public string ClientVersion
        {
            get => Get<string>("client_version");
            set => Set("client_version", value);
        }

        public Guid RavenfallUserId
        {
            get => Get<Guid>("ravenfall_id");
            set => Set("ravenfall_id", value);
        }

        public string RavenfallUserName
        {
            get => Get<string>("ravenfall_name");
            set => Set("ravenfall_name", value);
        }

        public string TwitchPubSubToken
        {
            get => Get<string>("twitch_pubsub");
            set => Set("twitch_pubsub", value);
        }

        public string TwitchUserId
        {
            get => Get<string>("twitch_id");
            set => Set("twitch_id", value);
        }

        public string TwitchUserName
        {
            get => Get<string>("twitch_name");
            set => Set("twitch_name", value);
        }
        public string KickUserId
        {
            get => Get<string>("kick_id");
            set => Set("kick_id", value);
        }

        public string KickUserName
        {
            get => Get<string>("kick_name");
            set => Set("kick_name", value);
        }

        public string DiscordUserId
        {
            get => Get<string>("discord_id");
            set => Set("discord_id", value);
        }
        public string DiscordUserName
        {
            get => Get<string>("discord_name");
            set => Set("discord_name", value);
        }

        public string YouTubeUserId
        {
            get => Get<string>("youtube_id");
            set => Set("youtube_id", value);
        }

        public string YouTubeUserName
        {
            get => Get<string>("youtube_name");
            set => Set("youtube_name", value);
        }

        public bool IsAdministrator
        {
            get => Get<bool>("is_administrator");
            set => Set("is_administrator", value);
        }

        public bool IsModerator
        {
            get => Get<bool>("is_moderator");
            set => Set("is_moderator", value);
        }

        public int PatreonTierLevel
        {
            get => Get<int>(nameof(PatreonTierLevel));
            set => Set(nameof(PatreonTierLevel), value);
        }

        public ChatMessageTransformation ChatMessageTransformation
        {
            get
            {
                var value = Get<string>(nameof(ChatMessageTransformation));
                if (Enum.TryParse<ChatMessageTransformation>(value, out var tr))
                {
                    return tr;
                }


                if (int.TryParse(value, out var n))
                {
                    return (ChatMessageTransformation)n;
                }

                return ChatMessageTransformation.Standard;
            }

            set => Set(nameof(ChatMessageTransformation), value);
        }

        public string ChatBotLanguage
        {
            get => Get<string>(nameof(ChatBotLanguage));
            set => Set(nameof(ChatBotLanguage), value);
        }

        public void Set<T>(string key, T value)
        {
            ReloadIfNecessary();
            dict[key] = value;

            try
            {
                var dir = System.IO.Path.GetDirectoryName(file);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                lock (ioMutex)
                {
                    System.IO.File.WriteAllText(file, Newtonsoft.Json.JsonConvert.SerializeObject(dict));
                }
            }
            catch (Exception exc)
            {
                // aww.
                lock (ioMutex)
                {
                    System.IO.File.WriteAllText(file + ".error", exc.ToString());
                }
            }
        }

        public bool TryGet<T>(string key, out T value)
        {
            // check if new settings file available, then force reload.
            ReloadIfNecessary();

            value = default;
            var obj = this[key];
            if (obj == null)
            {
                return false;
            }

            if (obj is T t)
            {
                value = t;
                return true;
            }

            if (typeof(T) == typeof(string)) // (obj is not string) << covered by previous obj is T t
            {
                value = (T)(object)obj.ToString();
                return true;
            }

            if (obj is string str)
            {
                if (typeof(T) == typeof(Guid))
                {
                    if (Guid.TryParse(str, out var res))
                    {
                        value = (T)(object)res;
                        return true;
                    }
                }

                try
                {
                    value = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
                    return true;
                }
                catch { }
            }

            try
            {
                value = (T)obj;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ReloadIfNecessary()
        {
            try
            {
                if (string.IsNullOrEmpty(file))
                {
                    return; //this is a empty settings file when no settings are available.
                }

                lock (ioMutex)
                {
                    var lastWriteTime = System.IO.File.GetLastWriteTimeUtc(file);
                    if (lastWriteTime >= loadedTime)
                    {
                        var json = System.IO.File.ReadAllText(file);
                        dict = new ConcurrentDictionary<string, object>(Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentDictionary<string, object>>(json));
                        loadedTime = DateTime.UtcNow;
                    }
                }
            }
            catch { }
        }

        public T Get<T>(string key, T defaultValue)
        {
            if (TryGet<T>(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        public T Get<T>(string key)
        {
            return Get<T>(key, default);
        }

        public object this[string key]
        {
            get
            {
                ReloadIfNecessary();

                if (dict.TryGetValue(key, out var value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                ReloadIfNecessary();

                dict[key] = value;
            }
        }
    }
}
