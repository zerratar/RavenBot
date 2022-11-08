using Microsoft.Extensions.Logging;
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

        private PubSubState state;

        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;
        public event EventHandler<OnListenResponseArgs> OnListenFailBadAuth;
        public bool IsConnected => client != null && state >= PubSubState.Connected;
        public bool IsReady => client != null && state == PubSubState.Ready;
        public bool IsAuthOK => token != null && !token.BadAuth;


        public TwitchPubSubClient(ILogger logger, PubSubToken token)
        {
            token.OnUpdated += OnTokenUpdated;

            this.logger = logger;
            this.token = token;

            CreateClient();
            Connect();
        }

        private async void OnTokenUpdated(object sender, EventArgs e)
        {
            if (state == PubSubState.Connecting)
            {
                CreateClient();
            }

            if (state == PubSubState.Disconnected || state == PubSubState.Connected)
            {
                logger.LogWarning("[TWITCH] Recieved new pubsub token for (Username: '" + token.UserName + "') trying to reconnect.");
                Connect();
                await Task.Delay(1000);
            }
        }

        private void CreateClient()
        {
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
                }
                catch { }
            }

            this.client = new TwitchPubSub();
            client.OnPubSubServiceConnected += Client_OnPubSubServiceConnected;
            client.OnPubSubServiceClosed += Client_OnPubSubServiceClosed;
            client.OnPubSubServiceError += Client_OnPubSubServiceError;
            client.OnListenResponse += Client_OnListenResponse;
            client.OnChannelPointsRewardRedeemed += Client_OnChannelPointsRewardRedeemed;
            state = PubSubState.Disconnected;
        }

        public string getChannel()
        {
            return token.UserName;
        }

        private void Connect()
        {
            try
            {
                if (token.BadAuth || string.IsNullOrEmpty(token.Token))
                {
                    return;
                }

                state = PubSubState.Connecting;
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

            state = PubSubState.Disconnected;

            if (e.Exception is not OperationCanceledException)
            {
                logger.LogError("[TWITCH] PubSub Error (Username: " + token.UserName + " Exception: " + e.Exception.Message + ")");
            }

            if (wasReady && !token.BadAuth)
            {
                logger.LogWarning("[TWITCH] Attempting to Reconnect to PubSub (Username: " + token.UserName + ")");
                await Task.Delay(1000);
                Connect();
            }
            else
            {
                logger.LogWarning("[TWITCH] Rejecting Reconnect attempt to PubSub (Username: " + token.UserName + ")");
            }
        }

        private async void Client_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            state = PubSubState.Disconnected;
            logger.LogError("[TWITCH] PubSub Connection Closed for " + token.UserName);
            logger.LogWarning("[TWITCH] Attempting to Reconnect to PubSub (Username: " + token.UserName + ")");
            await Task.Delay(1000);
            Connect();
        }

        private void Client_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            try
            {
                state = PubSubState.Authenticating;

                //logger.LogDebug("[TWITCH] Connected To PubSub (Username: " + token.UserName + ")");                
                if (string.IsNullOrEmpty(token.Token))
                {
                    state = PubSubState.Connected;
                    return;
                }

                client.SendTopics(token.Token);
                logger.LogDebug("[TWITCH] Sent PubSub Topics (Username: " + token.UserName + " Token: " + token.Token + ")");
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Unable To Send PubSub Topics (Username: " + token.UserName + " Exception: " + exc + ")");
            }
        }

        private void Client_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (e.Successful)
            {
                state = PubSubState.Ready;
                token.UnverifiedToken = null;
                logger.LogDebug("[TWITCH] PubSub Listen Success (Topic: '" + e.Topic + "' Username: '" + token.UserName + "')");
            }
            else
            {
                state = PubSubState.Connected;
                logger.LogError("[TWITCH] PubSub Listen failed. (Topic: '" + e.Topic + "', Username: '" + token.UserName + "' Error: '" + e.Response.Error + "')");
                if (e.Response.Error == "ERR_BADAUTH")
                {
                    // Remove the token, we don't want to use this one.
                    token.Token = token.UnverifiedToken;
                    token.BadAuth = true;
                    OnListenFailBadAuth?.Invoke(this, e);
                }
            }
        }

        private void Client_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            state = PubSubState.Ready;
            logger.LogDebug("[TWITCH] PubSub Reward Redeemed (Channel: " + token.UserName + " Title: " + e.RewardRedeemed.Redemption.Reward.Title + ")");
            OnChannelPointsRewardRedeemed?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            state = PubSubState.Disconnected;
            logger.LogDebug("[TWITCH] PubSub Disposed (Username: " + token.UserName + ")");

            token.OnUpdated -= OnTokenUpdated;

            UnsubscribeClient();

            disposed = true;
            try
            {
                client.Disconnect();
            }
            catch
            {
            }
        }

        private void UnsubscribeClient()
        {
            client.OnPubSubServiceConnected -= Client_OnPubSubServiceConnected;
            client.OnChannelPointsRewardRedeemed -= Client_OnChannelPointsRewardRedeemed;
            client.OnPubSubServiceClosed -= Client_OnPubSubServiceClosed;
            client.OnPubSubServiceError -= Client_OnPubSubServiceError;
            client.OnListenResponse -= Client_OnListenResponse;
        }
    }

    public enum PubSubState
    {
        Disconnected,
        Connecting,
        Connected,
        Authenticating,
        Ready
    }
}
