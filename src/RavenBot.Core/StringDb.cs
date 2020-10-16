using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RavenBot.Core
{
    public class StringDb
    {
        private static readonly char[] WordSeparators = new char[] { ' ', '.', ',', ';', '-', '!' };
        private const string ArgOpen = "{";
        private const string ArgClose = "}";

        private const string FormatsDbFileName = "message-format.json";
        private const string MessageDbFileName = "msgdb.dat";

        private readonly ConcurrentDictionary<int, HashSet<string>> messages = new ConcurrentDictionary<int, HashSet<string>>();
        private readonly ConcurrentDictionary<string, FormatValue> formats = new ConcurrentDictionary<string, FormatValue>();
        private readonly string dbDirectory;
        private readonly object saveMutex = new object();

        public StringDb(string dbDirectory = "")
        {
            this.dbDirectory = dbDirectory;
        }

        public void Load()
        {
            var msgdb = GetFilePath(MessageDbFileName);
            var formatsdb = GetFilePath(FormatsDbFileName);

            if (File.Exists(msgdb))
            {
                var msgs = JsonConvert.DeserializeObject<Dictionary<int, HashSet<string>>>(File.ReadAllText(msgdb));
                foreach (var msg in msgs)
                {
                    messages[msg.Key] = msg.Value;
                }
            }

            if (File.Exists(formatsdb))
            {
                var fmts = JsonConvert.DeserializeObject<Dictionary<string, FormatValue>>(File.ReadAllText(formatsdb));
                foreach (var format in fmts)
                {
                    formats[format.Key] = format.Value;
                }
            }
        }

        public void Save()
        {
            lock (saveMutex)
            {
                var msgdb = GetFilePath(MessageDbFileName);
                var formatsdb = GetFilePath(FormatsDbFileName);
                var dir = GetDirectoryPath(dbDirectory);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (File.Exists(msgdb)) File.Delete(msgdb);
                File.WriteAllText(msgdb, JsonConvert.SerializeObject(messages));
                if (File.Exists(formatsdb)) File.Delete(formatsdb);
                File.WriteAllText(formatsdb, JsonConvert.SerializeObject(formats, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                }));
            }
        }

        private string GetDirectoryPath(string dir)
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var assemblyDirectory = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(codeBase).Path));
            return Path.Combine(assemblyDirectory, dbDirectory);
        }

        private string GetFilePath(string file)
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var assemblyDirectory = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(codeBase).Path));
            return Path.Combine(assemblyDirectory, dbDirectory, file);
        }

        public void Add(string msg)
        {
            var wordCount = GetWordCount(msg);
            if (!messages.TryGetValue(wordCount, out var hs))
                hs = messages[wordCount] = new HashSet<string>();
            hs.Add(msg);
        }

        public void ResetKeyFormat(string key)
        {
            if (this.formats.TryGetValue(key, out var format))
            {
                format.Override = null;
            }
        }

        public void SetKeyFormat(string key, string newFormat)
        {
            if (this.formats.TryGetValue(key, out var format))
                format.Override = newFormat;
        }

        public string GetFormatKey(string message, string separator = "")
        {
            if (TryGetFormat(message, out var format))
            {
                return GenerateFormatKey(format, separator);
            }

            return message;
        }

        private static string GenerateFormatKey(string format, string separator = "")
        {
            var output = string.Join(separator, format
                .Split(WordSeparators)
                .Select(x => x.Length > 1
                ? (char.ToUpper(x[0]) + x.Substring(1))
                : x.Length > 0 ? char.ToUpper(x[0]).ToString()
                : String.Empty)
                .Where(x => !string.IsNullOrEmpty(x)));

            for (var i = 0; i < 9999; ++i)
            {
                var before = output;
                output = output.Replace(ArgOpen + i + ArgClose, "X");
                if (output == before) break;
            }

            return output;
        }

        public string GetFormatByKey(string formatKey)
        {
            if (formats.TryGetValue(formatKey, out var format))
                return format.Override ?? format.Original;
            return null;
        }

        public string KeyFormat(string key, string message)
        {
            var newFormat = GetFormatByKey(key);
            return Format(message, newFormat);
        }

        public string Format(string msg, string newFormat)
        {
            if (TryGetFormat(msg, out var format, out var args))
            {
                return string.Format(newFormat, args.Take(newFormat.Count(x => x == '{')).ToArray());
            }

            if (!string.IsNullOrEmpty(newFormat))
                return newFormat;

            return msg;
        }

        public bool TryGetFormat(string msg, out string output)
        {
            return TryGetFormat(msg, out output, out _);
        }

        public bool TryGetFormat(string msg, out string output, out string[] arguments)
        {
            Add(msg);
            messages.TryGetValue(GetWordCount(msg), out var hs);
            output = msg;
            arguments = new string[0];
            foreach (var db in hs)
            {
                var diff = DiffWords(msg, db);
                if (diff.Deletions.Count >= 0)
                {
                    // high chance its correct.
                    arguments = diff.Deletions.ToArray();
                    for (var i = 0; i < diff.Deletions.Count; ++i)
                    {
                        output = output.Replace(diff.Deletions[i], ArgOpen + i + ArgClose);
                    }
                    break;
                }
            }

            var formatFound = output != msg;
            //if (formatFound)
            //{
            var key = formatFound ? GenerateFormatKey(output) : output;
            if (!formats.ContainsKey(key))
                formats[key] = new FormatValue { Original = output };
            //}

            return formatFound;
        }

        public class DiffResult
        {
            public IList<string> Additions { get; } = new List<string>();
            public IList<string> Deletions { get; } = new List<string>();
            public IList<string> All { get; } = new List<string>();

            public override string ToString()
            {
                var str = new StringBuilder();
                foreach (var a in All)
                    str.Append(a + Environment.NewLine);
                return str.ToString();
            }
        }

        private static DiffResult DiffWords(string leftText, string rightText)
        {
            var leftWords = GetWords(leftText);
            var rightWords = GetWords(rightText);
            var diff = new DiffResult();
            if (leftWords.Length != rightWords.Length)
                return diff; // must be same amount of words.

            for (var i = 0; i < leftWords.Length; i++)
            {
                if (rightWords.Length > i && !rightWords[i].Equals(leftWords[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    diff.Deletions.Add(leftWords[i]);
                    diff.Additions.Add(rightWords[i]);
                    diff.All.Add("-" + leftWords[i]);
                    diff.All.Add("+" + rightWords[i]);
                }
                else
                {
                    diff.All.Add("=" + leftWords[i]);
                }
            }

            return diff;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static int GetWordCount(string msg) => GetWords(msg).Length;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static string[] GetWords(string msg) => msg.Split(WordSeparators);

        private class FormatValue
        {
            public string Original { get; set; }
            public string Override { get; set; }
        }
    }
}
