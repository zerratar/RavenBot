using System;
using System.Linq;
using RavenBot.Core.Repositories;

namespace RavenBot.Core.Chat.Twitch
{

    public class TwitchUserStore : FileBasedRepository<TwitchUser>, ITwitchUserStore
    {
        public TwitchUserStore()
            : base("E:\\stream\\twitch-users.json")
        {
        }

        public ITwitchUser Get(string username)
        {
            lock (Mutex)
            {
                var user = Items.FirstOrDefault(x => x.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (user == null)
                {
                    user = new TwitchUser(username, null, 1000);
                    Store(user);
                }
                return new StoreBoundTwitchUser(this, user);
            }
        }

        private class StoreBoundTwitchUser : ITwitchUser
        {
            private readonly TwitchUserStore store;
            private readonly ITwitchUser user;

            public StoreBoundTwitchUser(
                TwitchUserStore store,
                ITwitchUser user)
            {
                this.store = store;
                this.user = user;
            }

            public string Name => user.Name;
            public string Alias => user.Alias;
            public long Credits => user.Credits;
            public bool CanAfford(long cost) => user.CanAfford(cost);

            public void RemoveCredits(long amount)
            {
                user.RemoveCredits(amount);
                store.Save();
            }

            public void AddCredits(long amount)
            {
                user.AddCredits(amount);
                store.Save();
            }

            public bool CanUseCommand(string command)
            {
                return user.CanUseCommand(command);
            }

            public void UseCommand(string command, TimeSpan cooldown)
            {
                user.UseCommand(command, cooldown);
            }

            public TimeSpan GetCooldown(string command)
            {
                return user.GetCooldown(command);
            }
        }
    }
}