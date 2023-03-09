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
        private readonly PubSubToken token;

        private bool disposed;
        private PubSubState state;

        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;
        public event EventHandler<TwitchPubSubClient> OnDispose;
        public bool IsConnected => client != null && state >= PubSubState.Connected;
        public bool IsReady => client != null && state == PubSubState.Ready;
        public bool IsAuthOK => token != null && !token.BadAuth;
        public PubSubToken Token => token;

        private bool allowReconnect = true;
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
                logger.LogWarning("[TWITCH] Recieved new pubsub token for (Username: " + token.UserName + ") trying to reconnect.");
                Connect();
                await Task.Delay(1000);
            }
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

        public string getChannel()
        {
            return token.UserName;
        }

        private void Connect()
        {
            if (disposed)
            {
                return;
            }

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
                logger.LogError("[TWITCH] Unable to connect To pubsub (Username: " + token.UserName + " Error: " + exc.Message + ")");
            }
        }

        private async Task ReconnectAsync()
        {
            if (!allowReconnect)
            {
                Dispose();
                return;
            }

            logger.LogWarning("[TWITCH] Attempting to Reconnect to PubSub (Username: " + token.UserName + ")");
            await Task.Delay(1000);
            Connect();
        }

        private async void Client_OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            var wasReady = IsReady;

            state = PubSubState.Disconnected;

            if (e.Exception is not OperationCanceledException)
            {
                logger.LogError("[TWITCH] PubSub Error (Username: " + token.UserName + " Exception: " + e.Exception.Message + ")");
            }

            allowReconnect = allowReconnect && wasReady && !token.BadAuth;
            await ReconnectAsync();
        }

        private async void Client_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            state = PubSubState.Disconnected;
            logger.LogError("[TWITCH] PubSub Connection Closed for " + token.UserName);
            await ReconnectAsync();
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
                allowReconnect = true;
                state = PubSubState.Ready;
                token.UnverifiedToken = null;
                logger.LogDebug("[TWITCH] PubSub Listen Success (Topic: " + e.Topic + " Username: " + token.UserName + ")");
            }
            else
            {
                state = PubSubState.Connected;
                logger.LogError("[TWITCH] PubSub Listen failed. (Topic: " + e.Topic + ", Username: " + token.UserName + " Error: " + e.Response.Error + ")");

                if (e.Response.Error == "ERR_BADAUTH")
                {
                    allowReconnect = false;
                    // Remove the token, we don't want to use this one.
                    token.Token = token.UnverifiedToken;
                    token.BadAuth = true;
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

            disposed = true;
            allowReconnect = false;
            state = PubSubState.Disposed;

            logger.LogDebug("[TWITCH] PubSub Disposed (Username: " + token.UserName + ")");
            token.OnUpdated -= OnTokenUpdated;

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
