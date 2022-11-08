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
                i.OnDispose -= OnClientDisposed;
                i.Dispose();
            }

            pubsubClients.Clear();
            awaitingPubSubAccess.Clear();
        }

        public void Disconnect(string channel, bool logRemoval = true)
        {
            if (!TryRemoveClient(channel, out var client))
            {
                return;
            }

            if (logRemoval) //silent the disconnect debug. We don't disconnect if we never connected in the first place
            {
                logger.LogDebug("[TWITCH] Disconnected from PubSub (Channel: " + channel + ")");
            }

            if (client != null)
            {
                client.Dispose();
            }
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

        public void PubSubConnect(string channel)
        {
            var key = channel.ToLower();

            if (pubsubClients.TryGetValue(key, out var client))
            {
                if (!client.IsReady)
                {
                    logger.LogDebug("[TWITCH] PubSub Client Already Exists (Channel: " + channel + " Connected: " + client.IsConnected + " Ready: " + client.IsReady + ")");

                    Disconnect(channel, client.IsAuthOK);
                }
                return;
            }

            var token = pubsubTokenRepo.GetToken(channel);

            if (token == null)
            {
                logger.LogDebug("[RVNFLL] No Token Found (Channel: " + channel + ")");
                return;
            }

            if (token.BadAuth)
            {
                logger.LogDebug("[RVNFLL] Token Previously Failed Auth (Channel: " + channel + ")");
                return;
            }

            if (awaitingPubSubAccess.Contains(key))
            {
                awaitingPubSubAccess.Remove(key);
                messageBus.Send("pubsub_init", channel);
            }

            client = new TwitchPubSubClient(logger, token);
            client.OnChannelPointsRewardRedeemed += Client_OnChannelPointsRewardRedeemed;
            client.OnDispose += OnClientDisposed;
            pubsubClients[channel.ToLower()] = client;
        }

        private void OnClientDisposed(object sender, TwitchPubSubClient e)
        {
            TryRemoveClient(e);
        }

        private void Client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            OnChannelPointsRewardRedeemed?.Invoke(this, e);
        }

        private bool TryRemoveClient(string channel, out TwitchPubSubClient client)
        {
            channel = channel.ToLower();
            if (!pubsubClients.TryGetValue(channel, out client))
            {
                return false;
            }

            return TryRemoveClient(client);
        }

        private bool TryRemoveClient(TwitchPubSubClient client)
        {
            client.OnChannelPointsRewardRedeemed -= Client_OnChannelPointsRewardRedeemed;
            client.OnDispose -= OnClientDisposed;
            return pubsubClients.TryRemove(client.Token.UserName.ToLower(), out _);
        }
    }
}
