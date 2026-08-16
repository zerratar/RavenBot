using Microsoft.Extensions.Logging;
using ROBot.Core.GameServer;
using Shinobytes.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Chat.Twitch.PubSub
{
    public class TwitchPubSubData
    {
        public RemoteGameSessionInfo SessionInfo { get; set; }
        public string TwitchUserId { get; set; }
        public string PubSubToken { get; set; }
        public bool IsReady { get; set; }
        public bool IsConnecting { get; set; }
        public bool IsBadAuth { get; internal set; }
    }

    public class TwitchPubSubManager : ITwitchPubSubManager
    {
        private const bool enabled = false;
        private readonly IMessageBus messageBus;
        private readonly ILogger logger;
        private readonly IMessageBusSubscription subscription;

        private TwitchPubSub pubsub;

        //private ConcurrentDictionary<string, TwitchPubSubClient> pubsubClients
        //    = new ConcurrentDictionary<string, TwitchPubSubClient>();

        private ConcurrentDictionary<string, TwitchPubSubData> pubsubs
            = new ConcurrentDictionary<string, TwitchPubSubData>();

        private bool isPubsubConnected;

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
            if (info == null||!enabled)
                return;

            if (info.Settings != null)
            {
                try
                {
                    var twitchUserId = "";
                    if (info.Settings.TryGetValue("twitch_id", out var tid))
                        twitchUserId = tid.ToString();

                    var twitchPubSub = "";
                    if (info.Settings.TryGetValue("twitch_pubsub", out var ps))
                        twitchPubSub = ps.ToString();

                    if (info.Owner == null)
                        return;

                    var user = info.Owner.Username;
                    if (string.IsNullOrEmpty(user))
                        return;

                    var key = info.Owner.Username.ToLower();
                    if (pubsubs.TryGetValue(key, out var existing))
                    {
                        if (existing.IsBadAuth && existing.PubSubToken != twitchPubSub)
                        {
                            existing.IsBadAuth = false;
                        }

                        existing.PubSubToken = twitchPubSub;
                        existing.SessionInfo = info;
                        existing.TwitchUserId = twitchUserId;
                    }
                    else
                    {
                        pubsubs[key] = new TwitchPubSubData { PubSubToken = twitchPubSub, SessionInfo = info, TwitchUserId = twitchUserId };
                    }
                    PubSubConnect(info.Owner.Username);
                }
                catch (Exception exc)
                {
                    logger.LogError("[TWITCH] Error while processing PubSub data. " + exc.Message);
                }
            }
        }

        public string GetActivationLink()
        {
            //return GetAccessTokenRequestUrl(GenerateValidationToken());
            return "https://www.ravenfall.stream/api/auth/activate-pubsub";
        }

        public void Dispose()
        {
            subscription.Unsubscribe();
            //foreach (var i in pubsubClients.Values)
            //{
            //    i.OnDispose -= OnClientDisposed;
            //    i.Dispose();
            //}

            if (pubsub != null)
            {
                try
                {
                    pubsub.Disconnect();
                }
                catch { }
            }

            foreach (var data in pubsubs)
            {
                data.Value.IsReady = false;
            }
            //pubsubClients.Clear();
        }

        public void Disconnect(string channel, bool logRemoval = true)
        {
            if (pubsubs.TryGetValue(channel, out var d))
            {
                d.IsReady = false;
            }
        }

        public bool IsReady(string channel)
        {
            var key = channel.ToLower();
            if (pubsubs.TryGetValue(key, out var d))
                return d.IsReady;
            return false;
        }

        public void PubSubConnect(string channel)
        {
            if (!enabled) return;
            var key = channel.ToLower();

            if (!pubsubs.TryGetValue(key, out var data))
            {
                return;
            }

            if (data == null)
            {
                logger.LogDebug("[RVNFLL] No Token Found (Channel: " + channel + ")");
                return;
            }

            // Check if we are connected to pubsub, if not, connect.
            // on connection, listen to the topic provided here

            if (this.pubsub == null)
            {
                this.pubsub = new TwitchPubSub();
                this.pubsub.OnPubSubServiceConnected += Client_OnPubSubServiceConnected;
                this.pubsub.OnPubSubServiceClosed += Client_OnPubSubServiceClosed;
                this.pubsub.OnPubSubServiceError += Client_OnPubSubServiceError;
                this.pubsub.OnListenResponse += Client_OnListenResponse;
                this.pubsub.OnChannelPointsRewardRedeemed += Client_OnChannelPointsRewardRedeemed;
                this.pubsub.Connect();
            }

            else if (isPubsubConnected)
            {
                ListenToPubsubTopics();
            }
        }

        private void ListenToPubsubTopics()
        {
            foreach (var p in pubsubs)
            {
                if (!p.Value.IsReady && !p.Value.IsConnecting && !p.Value.IsBadAuth)
                {
                    p.Value.IsConnecting = true;
                    p.Value.IsReady = true;
                    logger.LogInformation("[TWITCH] Send Topics: " + p.Value.PubSubToken + " (username: " + p.Value.SessionInfo.Owner.Username + ")");
                    pubsub.ListenToChannelPoints(p.Value.TwitchUserId);
                    pubsub.SendTopics(p.Value.PubSubToken);
                }
            }
        }

        private void Client_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            try
            {
                //state = PubSubState.Authenticating;
                isPubsubConnected = true;

                logger.LogInformation("[TWITCH] Connected to PubSub Service.");

                ListenToPubsubTopics();

                //logger.LogDebug("[TWITCH] Sent PubSub Topics (Username: " + pubsub.SessionInfo.Owner.Username + " Token: " + pubsub.PubSubToken + ")");
            }
            catch (Exception exc)
            {
            }
        }

        private void Client_OnListenResponse(object sender, OnListenResponseArgs e)
        {

            if (!e.Successful)
            {
                var isBadAuth = e.Response.Error == "ERR_BADAUTH";

                foreach (var p in pubsubs)
                {
                    if (p.Value.TwitchUserId == e.ChannelId)
                    {
                        p.Value.IsReady = false;
                        p.Value.IsConnecting = false;
                        p.Value.IsBadAuth = isBadAuth;
                        logger.LogError("[TWITCH] PubSub Listen failed. (Topic: " + e.Topic + ", Username: " + p.Value.SessionInfo.Owner.Username + " Error: " + e.Response.Error + ")");
                    }
                }

                //badAuth = false;
                //state = PubSubState.Ready;
                //logger.LogDebug("[TWITCH] PubSub Listen Success (Topic: " + e.Topic + " Username: " + pubsub.SessionInfo.Owner.Username + ")");
            }
            else
            {
                foreach (var p in pubsubs)
                {
                    if (p.Value.TwitchUserId == e.ChannelId)
                    {
                        logger.LogDebug("[TWITCH] PubSub Listen Success (Topic: " + e.Topic + " Username: " + p.Value.SessionInfo.Owner.Username + ")");
                        p.Value.IsConnecting = false;
                        p.Value.IsReady = true;
                    }
                }
            }
            //else
            //{
            //    state = PubSubState.Connected;
            //    logger.LogError("[TWITCH] PubSub Listen failed. (Topic: " + e.Topic + ", Username: " + pubsub.SessionInfo.Owner.Username + " Error: " + e.Response.Error + ")");

            //    if (e.Response.Error == "ERR_BADAUTH")
            //    {
            //        badAuth = true;
            //        // Remove the token, we don't want to use this one.
            //    }
            //}
        }
        private void Client_OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            isPubsubConnected = false;
            foreach (var p in pubsubs)
            {
                p.Value.IsReady = false;
            }


            if (e.Exception is not OperationCanceledException)
            {
                logger.LogError("[TWITCH] PubSub Error: " + e.Exception.Message + ")");
            }

        }

        private void Client_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            isPubsubConnected = false;
            foreach (var p in pubsubs)
            {
                p.Value.IsReady = false;
            }
        }

        private void Client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            OnChannelPointsRewardRedeemed?.Invoke(this, e);
        }
    }
}
