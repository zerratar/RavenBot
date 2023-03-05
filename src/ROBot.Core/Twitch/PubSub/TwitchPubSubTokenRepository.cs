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

        public const string GeneratedData = "../generated-data";
        private const string PubSubTokenDb = GeneratedData + "/pubsub-tokens.json";

        public TwitchPubSubTokenRepository(ILogger logger)
        {
            this.logger = logger;
            LoadTokens();
        }

        public PubSubToken AddOrUpdate(string userId, string userName, string token, bool? badAuth)
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

                // Update if there's a change
                bool tokenChange = (existing.Token != token);
                existing.UserName = userName;
                existing.BadAuth = !tokenChange && badAuth.GetValueOrDefault();

                if (string.IsNullOrEmpty(existing.Token))
                {
                    existing.Token = token;
                }

                existing.UnverifiedToken = token;
                existing.Update();

                return existing;
            }
            finally
            {
                SaveTokens();
            }
        }

        public PubSubToken AddOrUpdate(string userId, string userName, string token)
        {
            return AddOrUpdate(userId, userName, token, false);
        }

        private void SaveTokens()
        {
            try
            {
                lock (mutex)
                {
                    var data = Newtonsoft.Json.JsonConvert.SerializeObject(tokens);
                    var dir = System.IO.Path.GetDirectoryName(PubSubTokenDb);
                    if (!System.IO.Directory.Exists(dir))
                        System.IO.Directory.CreateDirectory(dir);

                    System.IO.File.WriteAllText(PubSubTokenDb, data);
                }
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Unable to Save tokens (File: " + PubSubTokenDb + " Exc: " + exc + ")");
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

                        // hax to allow making the nullable BadAuth into a normal bool.
                        data = data.Replace("\"BadAuth\":null", "\"BadAuth\":false");

                        var records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PubSubToken>>(data);
                        this.tokens.Clear();
                        this.tokens.AddRange(records);

                        logger.LogDebug("[TWITCH] PubSub Records Loaded (Count: " + records.Count + ")");
                    }
                    else
                    {
                        logger.LogDebug("[TWITCH] PubSub Does Not Exist. Skipping. (Token: " + PubSubTokenDb + ")");
                    }
                }
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Unable To Load (Token: " + PubSubTokenDb + " Exc: " + exc + ")");
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

        public PubSubToken GetToken(string channelOrUserId)
        {
            return GetByUserName(channelOrUserId) ?? GetById(channelOrUserId);
        }
    }
}
