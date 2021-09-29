using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RavenBot.Core
{
    public interface ICommandBindingProvider
    {
        //string[] Get(string key);
        string[] Get(string key, params string[] optionalKeys);
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

        public string[] Get(string key, params string[] optionalKeys)
        {
            if (bindings.TryGetValue(key, out var binding))
            {
                return binding.Aliases;
            }

            if (optionalKeys != null && optionalKeys.Length > 0)
            {
                foreach (var optKey in optionalKeys)
                {
                    if (bindings.TryGetValue(optKey, out var optBinding))
                    {
                        return optBinding.Aliases;
                    }
                }
            }

            var aliases = new List<string>() { key };
            if (optionalKeys != null && optionalKeys.Length > 0)
            {
                aliases.AddRange(optionalKeys);
            }

            bindings[key] = new CommandBinding { Key = key, Aliases = aliases.ToArray() };
            return aliases.ToArray();
        }
    }

    public class CommandBinding
    {
        public string Key { get; set; }
        public string[] Aliases { get; set; }

        public override string ToString()
        {
            return "Binding: " + Key + " - Aliases: " + string.Join(",", Aliases);
        }
    }
}