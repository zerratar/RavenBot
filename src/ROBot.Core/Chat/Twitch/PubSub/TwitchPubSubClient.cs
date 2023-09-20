using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Chat.Twitch.PubSub
{
    public class TwitchPubSubClient : IDisposable
    {
        private TwitchPubSub client;
        private readonly ILogger logger;
        private readonly TwitchPubSubData pubsub;

        private bool disposed;
        private PubSubState state;

        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;
        public event EventHandler<TwitchPubSubClient> OnDispose;
        public bool IsConnected => client != null && state >= PubSubState.Connected;
        public bool IsReady => client != null && state == PubSubState.Ready;
        public bool IsConnecting => state == PubSubState.Connecting || state == PubSubState.Authenticating;
        private bool allowReconnect = true;
        public TwitchPubSubClient(ILogger logger, TwitchPubSubData pubsub)
        {
            this.logger = logger;
            this.pubsub = pubsub;

            CreateClient();
            Connect();
        }

        public string GetInstanceKey()
        {
            return pubsub.SessionInfo.Owner.Username.ToLower();
        }

        private void CreateClient()
        {
            if (disposed)
            {
                return;
            }

            if (client != null)
            {
                // kill it with fire.
                try
                {
                    UnsubscribeClient();

                    if (state != PubSubState.Disconnected)
                    {
                        client.Disconnect();
                    }

                    client = null;
                }
                catch { }
            }

            client = new TwitchPubSub();
            client.OnPubSubServiceConnected += Client_OnPubSubServiceConnected;
            client.OnPubSubServiceClosed += Client_OnPubSubServiceClosed;
            client.OnPubSubServiceError += Client_OnPubSubServiceError;
            client.OnListenResponse += Client_OnListenResponse;
            client.OnChannelPointsRewardRedeemed += Client_OnChannelPointsRewardRedeemed;
            state = PubSubState.Disconnected;
        }

        private void Connect()
        {
            if (disposed)
            {
                return;
            }

            try
            {
                state = PubSubState.Connecting;
                logger.LogDebug("[TWITCH] Connecting to PubSub (Username: " + pubsub.SessionInfo.Owner.Username + ")");
                client.ListenToChannelPoints(pubsub.TwitchUserId);
                client.Connect();
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Unable to connect To pubsub (Username: " + pubsub.SessionInfo.Owner.Username + " Error: " + exc.Message + ")");
            }
        }

        private async Task ReconnectAsync()
        {
            if (!allowReconnect)
            {
                Dispose();
                return;
            }

            logger.LogWarning("[TWITCH] Attempting to Reconnect to PubSub (Username: " + pubsub.SessionInfo.Owner.Username + ")");
            await Task.Delay(1000);
            Connect();
        }

        private async void Client_OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            var wasReady = IsReady;

            state = PubSubState.Disconnected;

            if (e.Exception is not OperationCanceledException)
            {
                logger.LogError("[TWITCH] PubSub Error (Username: " + pubsub.SessionInfo.Owner.Username + " Exception: " + e.Exception.Message + ")");
            }

            allowReconnect = allowReconnect && wasReady;
            await ReconnectAsync();
        }

        private async void Client_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            state = PubSubState.Disconnected;
            logger.LogError("[TWITCH] PubSub Connection Closed for " + pubsub.SessionInfo.Owner.Username);
            await ReconnectAsync();
        }

        private void Client_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            try
            {
                state = PubSubState.Authenticating;

                client.SendTopics(pubsub.PubSubToken);
                //logger.LogDebug("[TWITCH] Sent PubSub Topics (Username: " + pubsub.SessionInfo.Owner.Username + " Token: " + pubsub.PubSubToken + ")");
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Unable To Send PubSub Topics (Username: " + pubsub.SessionInfo.Owner.Username + " Exception: " + exc + ")");
            }
        }

        private void Client_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (e.Successful)
            {
                allowReconnect = true;
                state = PubSubState.Ready;
                logger.LogDebug("[TWITCH] PubSub Listen Success (Topic: " + e.Topic + " Username: " + pubsub.SessionInfo.Owner.Username + ")");
            }
            else
            {
                state = PubSubState.Connected;
                logger.LogError("[TWITCH] PubSub Listen failed. (Topic: " + e.Topic + ", Username: " + pubsub.SessionInfo.Owner.Username + " Error: " + e.Response.Error + ")");

                if (e.Response.Error == "ERR_BADAUTH")
                {
                    allowReconnect = false;
                    // Remove the token, we don't want to use this one.
                }
            }
        }

        private void Client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            state = PubSubState.Ready;
            logger.LogDebug("[TWITCH] PubSub Reward Redeemed (Channel: " + pubsub.SessionInfo.Owner.Username + " Title: " + e.RewardRedeemed.Redemption.Reward.Title + ")");
            OnChannelPointsRewardRedeemed?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            allowReconnect = false;
            state = PubSubState.Disposed;

            logger.LogDebug("[TWITCH] PubSub Disposed (Username: " + pubsub.SessionInfo.Owner.Username + ")");

            UnsubscribeClient();

            try
            {
                client.Disconnect();
            }
            catch
            {
            }

            client = null;
            OnDispose?.Invoke(this, this);
        }

        private void UnsubscribeClient()
        {
            if (client != null)
            {
                client.OnPubSubServiceConnected -= Client_OnPubSubServiceConnected;
                client.OnChannelPointsRewardRedeemed -= Client_OnChannelPointsRewardRedeemed;
                client.OnPubSubServiceClosed -= Client_OnPubSubServiceClosed;
                client.OnPubSubServiceError -= Client_OnPubSubServiceError;
                client.OnListenResponse -= Client_OnListenResponse;
            }
        }
    }

    public enum PubSubState
    {
        Disconnected,
        Disposed,

        Connecting,
        Connected,
        Authenticating,
        Ready
    }
}
