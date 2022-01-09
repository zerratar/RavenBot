﻿using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Twitch
{
    public class TwitchPubSubClient : IDisposable
    {
        private TwitchPubSub client;
        private bool disposed;
        private readonly ILogger logger;
        private readonly PubSubToken token;

        private bool isConnected;
        private bool receivesChannelPointRewardDetails;

        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;

        public bool IsConnected => client != null && isConnected;

        public bool IsReady => client != null && isConnected && receivesChannelPointRewardDetails;

        public TwitchPubSubClient(ILogger logger, PubSubToken token)
        {
            this.logger = logger;
            this.token = token;
            this.client = new TwitchPubSub();

            client.OnPubSubServiceConnected += Client_OnPubSubServiceConnected;
            client.OnPubSubServiceClosed += Client_OnPubSubServiceClosed;
            client.OnPubSubServiceError += Client_OnPubSubServiceError;
            client.OnListenResponse += Client_OnListenResponse;
            client.OnChannelPointsRewardRedeemed += Client_OnChannelPointsRewardRedeemed;

            Connect();
        }

        private void Connect()
        {
            try
            {
                logger.LogDebug("[TWITCH] Connecting to PubSub (Username: " + token.UserName + ")");
                client.ListenToChannelPoints(token.UserId);
                client.Connect();
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Unable to connect To pubsub (Username: " + token.UserName + " Error: " + exc + ")");
            }
        }

        private async void Client_OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            var wasReady = IsReady;

            isConnected = false;
            receivesChannelPointRewardDetails = false;
            logger.LogError("[TWITCH] PubSub ERROR (Username: " + token.UserName + " Exception: " + e.Exception.Message + ")");

            if (wasReady)
            {
                logger.LogWarning("[TWITCH] Attempting to Reconnect to PubSub (Username: " + token.UserName + ")");
                await Task.Delay(1000);
                Connect();
            }
        }

        private async void Client_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            isConnected = false;
            receivesChannelPointRewardDetails = false;
            logger.LogError("[TWITCH] PubSub Connection Closed for " + token.UserName);

            logger.LogWarning("[TWITCH] Attempting to Reconnect to PubSub (Username: " + token.UserName + ")");
            await Task.Delay(1000);
            Connect();
        }

        private void Client_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            try
            {
                isConnected = true;
                logger.LogDebug("[TWITCH] Connected To PubSub (Username: " + token.UserName + ")");
                client.SendTopics(token.Token);
                logger.LogDebug("[TWITCH] Sent PubSub Topics (Username: " + token.UserName + " Token:" + token.Token + ")");
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Unable To Send PubSub Topics (Username: " + token.UserName + " Exception:" + exc + ")");
            }
        }

        private void Client_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (e.Successful)
            {
                receivesChannelPointRewardDetails = true;
                logger.LogDebug("[TWITCH] PubSub Listen Success (Topic: " + e.Topic + " Username: " + token.UserName + ")");
            }
            else
            {
                logger.LogError("[Twitch] PubSub Listen Unsuccessful  (Username:" + token.UserName + " Error:" + e.Response.Error + ")");
            }
        }

        private void Client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            isConnected = true;
            receivesChannelPointRewardDetails = true;
            logger.LogDebug("[TWITCH] PubSub Reward Redeemed (Channel: " + token.UserName + " Title: " + e.RewardRedeemed.Redemption.Reward.Title + ")");
            OnChannelPointsRewardRedeemed?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            isConnected = false;
            receivesChannelPointRewardDetails = false;
            logger.LogDebug("[Twitch] PubSub Disposed (Username: " + token.UserName + ")");

            client.OnPubSubServiceConnected -= Client_OnPubSubServiceConnected;
            client.OnChannelPointsRewardRedeemed -= Client_OnChannelPointsRewardRedeemed;
            client.OnPubSubServiceClosed -= Client_OnPubSubServiceClosed;
            client.OnPubSubServiceError -= Client_OnPubSubServiceError;
            client.OnListenResponse -= Client_OnListenResponse;
            disposed = true;
            try
            {
                client.Disconnect();
            }
            catch
            {
            }
        }
    }
}
