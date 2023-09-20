using Microsoft.Extensions.Logging;
using ROBot.Core.GameServer;
using Shinobytes.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Chat.Twitch.PubSub
{
    public class TwitchPubSubData
    {
        public RemoteGameSessionInfo SessionInfo { get; set; }
        public string TwitchUserId { get; set; }
        public string PubSubToken { get; set; }
    }

    public class TwitchPubSubManager : ITwitchPubSubManager
    {
        private const string TwitchClientID = "757vrtjoawg2rtquprnfb35nqah1w4";
        private const string TwitchRedirectUri = "https://id.twitch.tv/oauth2/authorize";
        private readonly Random random = new Random();
        private readonly IMessageBus messageBus;
        private readonly ILogger logger;
        private readonly IMessageBusSubscription subscription;

        private ConcurrentDictionary<string, TwitchPubSubClient> pubsubClients
            = new ConcurrentDictionary<string, TwitchPubSubClient>();

        private ConcurrentDictionary<string, TwitchPubSubData> pubsubs
            = new ConcurrentDictionary<string, TwitchPubSubData>();

        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;

        public TwitchPubSubManager(
            IMessageBus messageBus,
            ILogger logger)
        {
            this.messageBus = messageBus;
            this.logger = logger;

            this.subscription = messageBus.Subscribe<RemoteGameSessionInfo>("ravenfall_session", OnGameSessionInfoReceived);
        }

        private void OnGameSessionInfoReceived(RemoteGameSessionInfo info)
        {
            if (info.Settings != null)
            {
                var twitchUserId = "";
                if (info.Settings.TryGetValue("twitch_id", out var tid))
                    twitchUserId = tid.ToString();

                var twitchPubSub = "";
                if (info.Settings.TryGetValue("twitch_pubsub", out var ps))
                    twitchPubSub = ps.ToString();

                pubsubs[info.Owner.Username.ToLower()] = new TwitchPubSubData { PubSubToken = twitchPubSub, SessionInfo = info, TwitchUserId = twitchUserId };
                
                PubSubConnect(info.Owner.Username);
            }
        }

        public string GetActivationLink(string userId, string username)
        {
            //return GetAccessTokenRequestUrl(GenerateValidationToken());
            return "https://www.ravenfall.stream/api/auth/activate-pubsub";
        }

        public void Dispose()
        {
            subscription.Unsubscribe();
            foreach (var i in pubsubClients.Values)
            {
                i.OnDispose -= OnClientDisposed;
                i.Dispose();
            }

            pubsubClients.Clear();
        }

        public void Disconnect(string channel, bool logRemoval = true)
        {
            if (!TryRemoveClient(channel, out var client))
            {
                return;
            }

            //if (logRemoval) //silent the disconnect debug. We don't disconnect if we never connected in the first place
            //{
            //    logger.LogDebug("[TWITCH] Disconnected from PubSub (Channel: " + channel + ")");
            //}

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
                if (!client.IsReady && !client.IsConnecting)
                {
                    logger.LogDebug("[TWITCH] PubSub Client Already Exists (Channel: " + channel + " Connected: " + client.IsConnected + " Ready: " + client.IsReady + ")");

                    Disconnect(channel);
                }
                return;
            }

            if (!pubsubs.TryGetValue(key, out var pubsub))
            {
                return;
            }

            if (pubsub == null)
            {
                logger.LogDebug("[RVNFLL] No Token Found (Channel: " + channel + ")");
                return;
            }

            client = new TwitchPubSubClient(logger, pubsub);
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
            return pubsubClients.TryRemove(client.GetInstanceKey(), out _);
        }
    }
}
