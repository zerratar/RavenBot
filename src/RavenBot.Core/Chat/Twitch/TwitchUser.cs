﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RavenBot.Core.Chat.Twitch
{
    public class TwitchUser : ITwitchUser
    {
        public TwitchUser(string name, string @alias, long credits)
        {
            Name = name;
            Alias = alias;
            Credits = credits;
            usedCommands = new Dictionary<string, UsedCommandInfo>();
        }

        public string Name { get; }
        public string Alias { get; }
        public long Credits { get; private set; }

        private Dictionary<string, UsedCommandInfo> usedCommands { get; set; }

        public bool CanAfford(long cost)
        {
            return Credits >= cost;
        }

        public void RemoveCredits(long amount)
        {
            Credits -= amount;
        }

        public void AddCredits(long amount)
        {
            Credits += amount;
        }

        public bool CanUseCommand(string command)
        {
            if (usedCommands.TryGetValue(command, out var cmd))
            {
                var now = DateTime.Now;
                var elapsed = now - cmd.LastUsed;
                return elapsed >= cmd.Cooldown;
            }

            return true;
        }

        public void UseCommand(string command, TimeSpan cooldown)
        {
            if (usedCommands.TryGetValue(command, out var item))
            {
                item.Cooldown = cooldown;
                item.LastUsed = DateTime.Now;
            }
            else
            {
                usedCommands[command] = new UsedCommandInfo(command, DateTime.Now, cooldown);
            }
        }

        public TimeSpan GetCooldown(string command)
        {
            if (usedCommands.TryGetValue(command, out var item))
            {
                var now = DateTime.Now;
                var elapsed = now - item.LastUsed;
                return item.Cooldown - elapsed;
            }

            return TimeSpan.MinValue;
        }
    }

    public class UsedCommandInfo
    {
        public UsedCommandInfo(string command, DateTime lastUsed, TimeSpan cooldown)
        {
            Command = command;
            LastUsed = lastUsed;
            Cooldown = cooldown;
        }

        public string Command { get; }
        public DateTime LastUsed { get; set; }
        public TimeSpan Cooldown { get; set; }
    }
}
