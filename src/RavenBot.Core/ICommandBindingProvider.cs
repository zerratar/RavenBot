using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RavenBot.Core
{
    public interface ICommandBindingProvider
    {
        string[] Get(string key);
        void EnsureBindingsFile();
    }

    public class CommandBindingProvider : ICommandBindingProvider
    {
        private const string CommandBindingsFile = "commands.json";
        private readonly ConcurrentDictionary<string, CommandBinding> bindings
            = new ConcurrentDictionary<string, CommandBinding>();

        public CommandBindingProvider()
        {
            LoadCommandBindings();
        }

        public void EnsureBindingsFile()
        {
            if (System.IO.File.Exists(CommandBindingsFile))
                return;

            var json = JsonConvert.SerializeObject(bindings.Values.ToList());
            System.IO.File.WriteAllText(CommandBindingsFile, json);
        }

        private void LoadCommandBindings()
        {
            if (!System.IO.File.Exists(CommandBindingsFile))
                return;

            var bindingsJson = System.IO.File.ReadAllText(CommandBindingsFile);
            var b = JsonConvert.DeserializeObject<List<CommandBinding>>(bindingsJson);
            foreach (var binding in b)
            {
                bindings[binding.Key] = binding;
            }
        }

        public string[] Get(string key)
        {
            if (bindings.TryGetValue(key, out var binding))
            {
                return binding.Aliases;
            }

            bindings[key] = new CommandBinding { Key = key, Aliases = new string[] { key } };
            return new string[] { key };
        }
    }

    public class CommandBinding
    {
        public string Key { get; set; }
        public string[] Aliases { get; set; }
    }
}