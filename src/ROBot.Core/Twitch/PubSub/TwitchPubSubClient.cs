using Microsoft.Extensions.Logging;
using System;
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

        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;

        public TwitchPubSubClient(ILogger logger, PubSubToken token)
        {
            this.logger = logger;
            this.token = token;
            this.client = new TwitchPubSub();

            client.OnPubSubServiceConnected += Client_OnPubSubServiceConnected;
            client.OnListenResponse += Client_OnListenResponse;
            client.OnChannelPointsRewardRedeemed += Client_OnChannelPointsRewardRedeemed;
            try
            {
                client.ListenToChannelPoints(token.UserId);
                client.Connect();
            }
            catch (Exception exc)
            {
                logger.LogError("Unable to connect to pubsub for " + token.UserName + ": " + exc);
            }
        }

        private void Client_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            try
            {
                logger.LogDebug("Connected to PubSub (" + token.UserName + ")");
                client.SendTopics(token.Token);
                logger.LogDebug("Sent PubSub Topics for  " + token.UserName + ": " + token.Token);
            }
            catch (Exception exc)
            {
                logger.LogError("Unable to send pubsub topics for " + token.UserName + ": " + exc);
            }
        }

        private void Client_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                logger.LogError("Unable to listen to pubsub for " + token.UserName + ": " + e.Response.Error);
            }
            else
            {
                logger.LogDebug("PubSub " + e.Topic + " Successefull for " + token.UserName);
            }
        }

        private void Client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            logger.LogDebug("PubSub (" + token.UserName + ") Channel Point Reward Redeemed: " + e.RewardRedeemed.Redemption.Reward.Title);
            OnChannelPointsRewardRedeemed?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            logger.LogDebug("PubSub (" + token.UserName + ") disposed.");

            client.OnPubSubServiceConnected -= Client_OnPubSubServiceConnected;
            client.OnChannelPointsRewardRedeemed -= Client_OnChannelPointsRewardRedeemed;
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
