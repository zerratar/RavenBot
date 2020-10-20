using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RavenBot.Core
{
    public class StringProvider : IStringProvider
    {
        private const string stringsJson = "strings.json";
        private static readonly string stringsFile = System.IO.Path.Combine(App.GetStartupFolder(), stringsJson);
        private readonly ConcurrentDictionary<string, string> values = new ConcurrentDictionary<string, string>();
        private readonly object ioMutex = new object();

        public StringProvider()
        {
            LoadStrings();
        }

        private void LoadStrings()
        {
            lock (ioMutex)
            {
                try
                {
                    if (!System.IO.File.Exists(stringsFile)) return;
                    var content = System.IO.File.ReadAllText(stringsFile);
                    if (string.IsNullOrWhiteSpace(content))
                        content = "{}";
                    var val = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

                    foreach (var v in val)
                    {
                        values[v.Key] = v.Value;
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void SaveStrings()
        {
            lock (ioMutex)
            {
                try
                {
                    var content = JsonConvert.SerializeObject(values, Formatting.Indented);
                    System.IO.File.WriteAllText(stringsFile, content);
                }
                catch
                {
                    // ignored
                }
            }
        }

        public string Get(string key)
        {
            if (values.TryGetValue(key, out var value))
                return value ?? key; // null should be key value

            values[key] = null;
            SaveStrings();
            return key;
        }

        public void Override(string oldValue, string newValue)
        {
            if (newValue == null)
                values[oldValue] = oldValue;
            else
                values[oldValue] = newValue;
        }
    }
}