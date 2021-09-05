using Microsoft.Extensions.Logging;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Twitch
{
    public class TwitchPubSubManager : ITwitchPubSubManager
    {
        private const string TwitchClientID = "757vrtjoawg2rtquprnfb35nqah1w4";
        private const string TwitchRedirectUri = "https://id.twitch.tv/oauth2/authorize";
        private readonly Random random = new Random();
        private readonly IMessageBus messageBus;
        private readonly ILogger logger;
        private readonly ITwitchPubSubTokenRepository pubsubTokenRepo;

        private HashSet<string> awaitingPubSubAccess = new HashSet<string>();

        private ConcurrentDictionary<string, TwitchPubSubClient> pubsubClients
            = new ConcurrentDictionary<string, TwitchPubSubClient>();

        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;

        public TwitchPubSubManager(
            IMessageBus messageBus,
            ILogger logger,
            ITwitchPubSubTokenRepository repo)
        {
            this.messageBus = messageBus;
            this.logger = logger;
            this.pubsubTokenRepo = repo;
        }

        public string GetActivationLink(string userId, string username)
        {
            awaitingPubSubAccess.Add(username.ToLower());

            //return GetAccessTokenRequestUrl(GenerateValidationToken());
            return "https://www.ravenfall.stream/api/auth/activate-pubsub";
        }

        private string GenerateValidationToken()
        {
            return Convert.ToBase64String(Enumerable.Range(0, 20).Select(x =>
            (byte)((byte)(random.NextDouble() * ((byte)'z' - (byte)'a')) + (byte)'a')).ToArray());
        }

        private string GetAccessTokenRequestUrl(string validationToken)
        {
            return
                TwitchRedirectUri + "?response_type=token" +
                $"&client_id={TwitchClientID}" +
                $"&redirect_uri=https://www.ravenfall.stream/login/twitch" +
                $"&scope=user:read:email+bits:read+chat:read+chat:edit+channel:read:subscriptions+channel:read:redemptions+channel:read:predictions" +
                $"&state=pubsub{validationToken}&force_verify=true";
        }

        public void Dispose()
        {
            foreach (var i in pubsubClients.Values)
            {
                i.Dispose();
            }
            pubsubClients.Clear();
            awaitingPubSubAccess.Clear();
        }

        public void Disconnect(string channel)
        {
            if (!pubsubClients.TryGetValue(channel.ToLower(), out var client))
            {
                return;
            }

            logger.LogDebug("Disconnected from PubSub: " + channel);

            client.OnChannelPointsRewardRedeemed -= Client_OnChannelPointsRewardRedeemed;
            pubsubClients.TryRemove(channel.ToLower(), out _);
            client.Dispose();
        }


        public bool IsReady(string channel)
        {
            var key = channel.ToLower();
            if (!pubsubClients.TryGetValue(key, out var client))
            {
                return false;
            }

            return true;
        }

        public bool Connect(string channel)
        {
            logger.LogDebug("Connecting to pubsub for " + channel + "...");

            var key = channel.ToLower();
            if (pubsubClients.TryGetValue(key, out var client))
            {
                return true;
            }

            var token = pubsubTokenRepo.GetByUserName(channel) ?? pubsubTokenRepo.GetById(channel);
            if (token == null)
            {
                logger.LogWarning("Trying to connect to pubsub for " + channel + " but no token is available.");
                return false;
            }

            if (awaitingPubSubAccess.Contains(key))
            {
                awaitingPubSubAccess.Remove(key);
                messageBus.Send("pubsub_init", channel);
            }

            client = new TwitchPubSubClient(logger, token);
            client.OnChannelPointsRewardRedeemed += Client_OnChannelPointsRewardRedeemed;

            pubsubClients[channel.ToLower()] = client;
            return true;
        }

        private void Client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            OnChannelPointsRewardRedeemed?.Invoke(this, e);
        }

    }
}
