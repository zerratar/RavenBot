using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ROBot.Core.Twitch
{
    public class TwitchPubSubTokenRepository : ITwitchPubSubTokenRepository
    {
        private readonly List<PubSubToken> tokens = new List<PubSubToken>();
        private readonly object mutex = new object();
        private readonly ILogger logger;

        private const string PubSubTokenDb = "pubsub-tokens.json";

        public TwitchPubSubTokenRepository(ILogger logger)
        {
            this.logger = logger;
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
                    System.IO.File.WriteAllText(PubSubTokenDb, data);
                }
            }
            catch (Exception exc)
            {
                logger.LogError("Unable to save " + PubSubTokenDb + ":" + exc);
            }
        }

        private void LoadTokens()
        {
            try
            {
                lock (mutex)
                {
                    if (System.IO.File.Exists(PubSubTokenDb))
                    {
                        var data = System.IO.File.ReadAllText(PubSubTokenDb);
                        var records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PubSubToken>>(data);
                        this.tokens.Clear();
                        this.tokens.AddRange(records);
                        logger.LogDebug(records.Count + " pubsub records loaded.");
                    }
                    else
                    {
                        logger.LogDebug(PubSubTokenDb + " does not exist. Skipping");
                    }
                }
            }
            catch (Exception exc)
            {
                logger.LogError("Unable to load " + PubSubTokenDb + ":" + exc);
            }
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
