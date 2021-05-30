using System;
using System.Collections.Generic;
using System.Linq;

namespace ROBot.Core.Twitch
{
    public class TwitchPubSubTokenRepository : ITwitchPubSubTokenRepository
    {
        private readonly List<PubSubToken> tokens = new List<PubSubToken>();
        private readonly object mutex = new object();

        public TwitchPubSubTokenRepository()
        {
            LoadTokens();
        }

        public PubSubToken AddOrUpdate(string userId, string userName, string token)
        {
            try
            {
                var existing = GetById(userId);
                if (existing == null)
                {
                    existing = new PubSubToken();
                    existing.UserId = userId;
                    lock (mutex)
                    {
                        tokens.Add(existing);
                    }
                }

                existing.Token = token;
                existing.UserName = userName;
                return existing;
            }
            finally
            {
                SaveTokens();
            }
        }

        private void SaveTokens()
        {
            try
            {
                lock (mutex)
                {
                    var data = Newtonsoft.Json.JsonConvert.SerializeObject(tokens);
                    System.IO.File.WriteAllText("pubsub-tokens.json", data);
                }
            }
            catch { }
        }

        private void LoadTokens()
        {
            try
            {
                lock (mutex)
                {
                    if (System.IO.File.Exists("pubsub-tokens.json"))
                    {
                        var data = System.IO.File.ReadAllText("pubsub-tokens.json");
                        this.tokens.Clear();
                        this.tokens.AddRange(Newtonsoft.Json.JsonConvert.DeserializeObject<List<PubSubToken>>(data));
                    }
                }
            }
            catch { }
        }

        public PubSubToken GetById(string userId)
        {
            lock (mutex)
            {
                return tokens.FirstOrDefault(x => x.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
            }
        }

        public PubSubToken GetByUserName(string channel)
        {
            lock (mutex)
            {
                return tokens.FirstOrDefault(x => x.UserName.Equals(channel, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
