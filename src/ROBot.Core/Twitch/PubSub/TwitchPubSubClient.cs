using System;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Twitch
{
    public class TwitchPubSubClient : IDisposable
    {
        private TwitchPubSub client;
        private bool disposed;
        private readonly PubSubToken token;

        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;

        public TwitchPubSubClient(PubSubToken token)
        {
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
            catch { }
        }

        private void Client_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            try
            {
                client.SendTopics(token.Token);
            }
            catch { }
        }

        private void Client_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            //if (!e.Successful)
            //{
            //    logger.LogError(e.Response.Error);
            //}
            //else
            //{
            //    logger.LogDebug("PubSub Listen OK");
            //}
        }

        private void Client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            OnChannelPointsRewardRedeemed?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

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
