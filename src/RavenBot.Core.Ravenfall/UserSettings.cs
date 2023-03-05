﻿using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class UserSettings
    {
        private readonly string file;
        private readonly object ioMutex = new object();

        private System.DateTime loadedTime;
        private ConcurrentDictionary<string, object> dict;

        public UserSettings(string file, Dictionary<string, object> src)
        {
            this.file = file;
            loadedTime = System.DateTime.UtcNow;
            dict = new ConcurrentDictionary<string, object>(src);
        }

        public UserSettings(string file)
        {
            this.file = file;
            loadedTime = System.DateTime.UtcNow;
            dict = new ConcurrentDictionary<string, object>();
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
                if (System.Enum.TryParse<ChatMessageTransformation>(value, out var tr))
                {
                    return tr;
                }


                if (int.TryParse(value, out var n))
                {
                    return (ChatMessageTransformation)n;
                }

                return RavenBot.Core.Ravenfall.Commands.ChatMessageTransformation.Standard;
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
            catch (System.Exception exc)
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

            value = default(T);
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

        private void ReloadIfNecessary()
        {
            try
            {
                lock (ioMutex)
                {
                    var lastWriteTime = System.IO.File.GetLastWriteTimeUtc(file);
                    if (lastWriteTime >= loadedTime)
                    {
                        var json = System.IO.File.ReadAllText(file);
                        dict = new ConcurrentDictionary<string, object>(Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentDictionary<string, object>>(json));
                        loadedTime = System.DateTime.UtcNow;
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

    public enum ChatMessageTransformation : uint
    {
        Standard = 0,
        Personalize = 1,
        Translate = 2,
        TranslateAndPersonalize = 3
    }
}