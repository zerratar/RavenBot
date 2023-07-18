using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RavenBot.Core;
using RavenBot.Core.Chat;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using Shinobytes.Core;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;

namespace RavenBot
{
    public class TwitchBot : ITwitchBot, IMessageChat
    {
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IUserSettingsManager userSettingsManager;
        private readonly IRavenfallClient ravenfall;
        private readonly IUserProvider playerProvider;
        private readonly ITwitchUserStore userStore;
        private readonly IChatMessageFormatter messageFormatter;
        private readonly IMessageBus messageBus;
        private readonly ICommandProvider commandProvider;
        private readonly ICommandHandler commandHandler;
        private readonly IChannelProvider channelProvider;
        private readonly IConnectionCredentialsProvider credentialsProvider;
        private IMessageBusSubscription broadcastSubscription;
        private TwitchPubSub pubsub;
        private TwitchClient client;
        private bool isInitialized;
        private int reconnectDelay = 10000;
        private bool tryToReconnect = true;
        private bool disposed;
        private string pubsubToken;
        private readonly object mutex = new object();
        private readonly HashSet<string> newSubAdded = new HashSet<string>();
        private PubSubState pubsubState;
        private bool tryPubSubAuthWithOAuthToken;
        private readonly object pubsubListenMutex = new object();
        public TwitchBot(
            ILogger logger,
            IKernel kernel,
            IUserSettingsManager userSettingsManager,
            IRavenfallClient ravenfall,
            IUserProvider playerProvider,
            IChatMessageFormatter localizer,
            IMessageBus messageBus,
            ICommandProvider commandProvider,
            ICommandHandler commandHandler,
            IChannelProvider channelProvider,
            IConnectionCredentialsProvider credentialsProvider)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.userSettingsManager = userSettingsManager;
            this.ravenfall = ravenfall;
            this.playerProvider = playerProvider;
            this.messageFormatter = localizer;
            this.messageBus = messageBus;
            this.commandProvider = commandProvider;

            this.commandHandler = commandHandler;
            this.channelProvider = channelProvider;
            this.credentialsProvider = credentialsProvider;

            this.messageBus.Subscribe<string>("pubsub_token", data =>
            {
                File.WriteAllText("pubsub-data.dat", data);
                ListenToChannelPoints(logger, data);
            });

            this.messageBus.Subscribe<string>("streamer_userid_acquired", data =>
            {
                if (tryPubSubAuthWithOAuthToken)
                {
                    var credentials = credentialsProvider.Get();
                    ListenToChannelPoints(logger, data + "," + credentials.TwitchOAuth);
                    tryPubSubAuthWithOAuthToken = false;
                }
            });

            this.CreateTwitchClient();

            if (File.Exists("pubsub-data.dat"))
            {
                ListenToChannelPoints(logger, System.IO.File.ReadAllText("pubsub-data.dat"));
            }
            else
            {
                tryPubSubAuthWithOAuthToken = true;
            }

            ravenfall.ProcessAsync(Settings.UNITY_SERVER_PORT);
        }

        public bool CanRecieveChannelPointRewards => pubsubState == PubSubState.OK;

        private void ListenToChannelPoints(ILogger logger, string data)
        {
            lock (pubsubListenMutex)
            {
                if (pubsubState == PubSubState.Connecting || pubsubState == PubSubState.OK || pubsubState == PubSubState.Authenticating)
                {
                    return;
                }

                pubsubState = PubSubState.Connecting;

                try
                {
                    var d = data.Split(',');
                    pubsubToken = d[1];

                    logger.WriteDebug("Connecting to PubSub...");
                    pubsub.ListenToChannelPoints(d[0]);
                    pubsub.Connect();
                }
                catch (Exception exc)
                {
                    pubsubState = PubSubState.ConnectionFailed;
                    logger.WriteError(exc.ToString());
                }
            }
        }

        public void Start()
        {
            if (!kernel.Started) kernel.Start();
            EnsureInitialized();
            Subscribe();
            client.Connect();
        }

        public void Dispose()
        {
            if (disposed) return;
            Stop();
            disposed = true;
        }

        private async Task OnUserLeft(object sender, OnUserLeftArgs e)
        {
            if (!e.Channel.Contains(this.channelProvider.Get(), StringComparison.InvariantCultureIgnoreCase))
                return;

            this.messageBus.Send(nameof(UserLeftEvent), new UserLeftEvent(e.Username));
        }

        private async Task OnUserJoined(object sender, OnUserJoinedArgs e)
        {
            if (!e.Channel.Contains(this.channelProvider.Get(), StringComparison.InvariantCultureIgnoreCase))
                return;

            this.messageBus.Send(nameof(UserJoinedEvent), new UserJoinedEvent(e.Username));
        }

        private async Task OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Bits == 0) return;

            this.messageBus.Send(
                nameof(CheerBitsEvent),
                new CheerBitsEvent(
                    "twitch",
                    e.ChatMessage.Channel,
                    e.ChatMessage.Id,
                    e.ChatMessage.Username,
                    e.ChatMessage.DisplayName,
                    e.ChatMessage.IsModerator,
                    e.ChatMessage.IsSubscriber,
                    e.ChatMessage.IsVip,
                    e.ChatMessage.Bits)
            );

            this.Announce(Localization.Twitch.THANK_YOU_BITS, e.ChatMessage.DisplayName, e.ChatMessage.Bits);
        }

        private async Task OnCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            var uid = e.Command.ChatMessage.UserId;
            var settings = userSettingsManager.Get(uid, "twitch");
            await commandHandler.HandleAsync(this, new TwitchCommand(e.Command, settings.IsAdministrator, settings.IsModerator));
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            if (this.broadcastSubscription == null)
                this.broadcastSubscription = messageBus.Subscribe<GameMessageResponse>(MessageBus.Broadcast, Broadcast);

            client.Initialize(credentialsProvider.Get(), channelProvider.Get());

            isInitialized = true;
        }

        public void Broadcast(GameMessageResponse message)
        {
            if (message.Recipent.Platform == "system")
            {
                // system message
                SendMessage(message.Format, message.Args);
                return;
            }

            if (message.Recipent.Platform == "twitch")
            {
                SendReply(message.Format, message.Args, message.CorrelationId, message.Recipent.PlatformUserName);

                //if (!string.IsNullOrEmpty(message.CorrelationId))
                //{
                //    SendReply(message.Format, message.Args, message.CorrelationId);
                //    return;
                //}

                //SendMessage(message.Format, message.Args);
            }

            // Ignore other platforms for now.
            // that way we don't get spam from all platforms in all chats.
            //  SendMessage(message.Format, message.Args);
        }

        public void Announce(string format, params object[] args)
        {
            SendMessage(format, args);
        }

        public void SendReply(ICommand cmd, string message, params object[] args)
        {
            SendReply(message, args, cmd.CorrelationId, cmd.Mention);
        }

        public void SendReply(string format, object[] args, string correlationId, string mention)
        {
            if (!this.client.IsConnected || string.IsNullOrWhiteSpace(format))
                return;

            var channel = this.channelProvider.Get();
            if (client.JoinedChannels.Count == 0)
                client.JoinChannel(channel);

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            if (!string.IsNullOrEmpty(correlationId))
            {
                client.SendReply(channel, correlationId, msg);
                return;
            }

            if (!string.IsNullOrEmpty(mention))
            {
                if (!mention.StartsWith("@"))
                {
                    mention = "@" + mention;
                }

                msg = mention + ", " + msg;
            }

            SendMessage(msg);
        }

        public void SendMessage(string format, object[] args)
        {
            if (!this.client.IsConnected || string.IsNullOrWhiteSpace(format))
                return;

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            SendMessage(msg);
        }

        private void SendMessage(string msg)
        {
            var channel = this.channelProvider.Get();
            if (client.JoinedChannels.Count == 0)
                client.JoinChannel(channel);

            client.SendMessage(channel, msg);
        }

        private void CreateTwitchClient()
        {
            pubsub = new TwitchPubSub();

            var options = new ClientOptions(
                clientType: ClientType.Chat);

            /*
             
             new ClientOptions
            {
                ClientType = ClientType.Chat,
                MessagesAllowedInPeriod = 100
            }
             
             */

            client = new TwitchClient(new WebSocketClient(options));
        }

        private async Task OnReSub(object sender, OnReSubscriberArgs e)
        {
            this.messageBus.Send(nameof(UserSubscriptionEvent),
                new UserSubscriptionEvent(
                    "twitch",
                    e.Channel,
                    e.ReSubscriber.UserId,
                    e.ReSubscriber.Login,
                    e.ReSubscriber.DisplayName,
                    null,
                    e.ReSubscriber.IsModerator,
                    e.ReSubscriber.IsSubscriber,
                    e.ReSubscriber.Months,
                    false));

            //this.Broadcast("", Localization.Twitch.THANK_YOU_RESUB, e.ReSubscriber.DisplayName);
        }

        private async Task OnNewSub(object sender, OnNewSubscriberArgs e)
        {
            this.messageBus.Send(nameof(UserSubscriptionEvent),
                new UserSubscriptionEvent(
                    "twitch",
                    e.Channel,
                    e.Subscriber.UserId,
                    e.Subscriber.Login,
                    e.Subscriber.DisplayName,
                    null,
                    e.Subscriber.IsModerator,
                    e.Subscriber.IsSubscriber,
                    1, true));

            //this.Broadcast("", Localization.Twitch.THANK_YOU_SUB, e.Subscriber.DisplayName);
        }

        private async Task OnPrimeSub(object sender, OnCommunitySubscriptionArgs e)
        {
            this.messageBus.Send(nameof(UserSubscriptionEvent),
                new UserSubscriptionEvent(
                    "twitch",
                    e.Channel,
                    e.GiftedSubscription.UserId,
                    e.GiftedSubscription.Login,
                    e.GiftedSubscription.DisplayName,
                    null,
                    e.GiftedSubscription.IsModerator,
                    e.GiftedSubscription.IsSubscriber,
                    1, false));

            //this.Broadcast("", Localization.Twitch.THANK_YOU_SUB, e.GiftedSubscription.DisplayName);
        }

        private async Task OnGiftedSub(object sender, OnGiftedSubscriptionArgs e)
        {
            this.messageBus.Send(nameof(UserSubscriptionEvent),
            new UserSubscriptionEvent("twitch",
                e.Channel,
                e.GiftedSubscription.Id,
                e.GiftedSubscription.Login,
                e.GiftedSubscription.DisplayName,
                e.GiftedSubscription.MsgParamRecipientId,
                e.GiftedSubscription.IsModerator,
                e.GiftedSubscription.IsSubscriber,
                1,
                false));
            //this.Broadcast("", Localization.Twitch.THANK_YOU_GIFT_SUB, e.GiftedSubscription.DisplayName);
        }

        private async Task OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            logger.WriteDebug("Disconnected from the Twitch IRC Server");
            TryToReconnect();
        }

        private void TryToReconnect()
        {
            try
            {
                Unsubscribe();
                isInitialized = false;
                CreateTwitchClient();
                Start();
            }
            catch (Exception)
            {
                logger.WriteDebug($"Failed to reconnect to the Twitch IRC Server. Retry in {reconnectDelay}ms");
                Task.Run(async () =>
                {
                    await Task.Delay(reconnectDelay);

                    if (!tryToReconnect)
                        return;

                    TryToReconnect();
                });
            }
        }

        public void Stop()
        {
            if (kernel.Started) kernel.Stop();
            Unsubscribe();

            tryToReconnect = false;
            if (client.IsConnected)
                client.Disconnect();

            try
            {
                if (pubsubState != PubSubState.NotConnected)
                {
                    pubsubState = PubSubState.NotConnected;
                    pubsub.Disconnect();
                }
            }
            catch
            {
            }

            broadcastSubscription?.Unsubscribe();
        }

        private async Task OnConnected(object sender, OnConnectedArgs e)
        {
            logger.WriteDebug("Connected to Twitch IRC Server");
            messageBus.Send("twitch", "");
        }

        //private void OnReconnected(object sender, OnReconnectedEventArgs e)
        //{
        //    logger.WriteDebug("Reconnected to Twitch IRC Server");
        //    messageBus.Send("twitch", "");
        //}

        private async Task OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            this.Announce(Localization.Twitch.THANK_YOU_RAID, e.RaidNotification.DisplayName);
        }

        private void Subscribe()
        {
            client.OnChatCommandReceived += OnCommandReceived;
            client.OnMessageReceived += OnMessageReceived;
            client.OnConnected += OnConnected;
            client.OnReconnected += OnReconnected;
            client.OnDisconnected += OnDisconnected;
            client.OnUserJoined += OnUserJoined;
            client.OnUserLeft += OnUserLeft;
            client.OnGiftedSubscription += OnGiftedSub;
            client.OnCommunitySubscription += OnPrimeSub;
            client.OnNewSubscriber += OnNewSub;
            client.OnReSubscriber += OnReSub;
            client.OnRaidNotification += OnRaidNotification;
            client.OnConnectionError += OnConnectionError;
            pubsub.OnListenResponse += Pubsub_OnListenResponse;
            pubsub.OnPubSubServiceConnected += Pubsub_OnPubSubServiceConnected;
            pubsub.OnPubSubServiceError += Pubsub_OnPubSubServiceError;
            pubsub.OnPubSubServiceClosed += Pubsub_OnPubSubServiceClosed;
            pubsub.OnChannelPointsRewardRedeemed += Pubsub_OnChannelPointsRewardRedeemed;
        }

        private async Task OnReconnected(object sender, OnConnectedArgs e)
        {
            logger.WriteDebug("Reconnected to Twitch IRC Server");
            messageBus.Send("twitch", "");
        }

        private async Task OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            logger.WriteError("Error connecting to Twitch: " + e.Error + ". Maybe time to refresh the access token?");
        }

        private void Pubsub_OnPubSubServiceError(object sender, TwitchLib.PubSub.Events.OnPubSubServiceErrorArgs e)
        {
            logger.WriteError("PubSub Service Error: " + e.Exception);
        }

        private void Pubsub_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            pubsubState = PubSubState.NotConnected;
            logger.WriteWarning("Disconnected from PubSub");
        }

        private void Pubsub_OnChannelPointsRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e)
        {
            var player = playerProvider.Get(e.RewardRedeemed.Redemption.User.Login);
            var cmd = commandProvider.GetCommand(player, e.RewardRedeemed.Redemption.ChannelId, e.RewardRedeemed.Redemption.Reward.Title, e.RewardRedeemed.Redemption.Reward.Prompt);

            logger.WriteDebug("Channel Point Reward: " + e.RewardRedeemed.Redemption.Reward.Title + " - Redeemed by " + player.Username + " for " + e.RewardRedeemed.Redemption.Reward.Cost + "pts");

            if (cmd != null)
            {
                commandHandler.HandleAsync(this, cmd);
            }
        }

        private void Pubsub_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                pubsubState = PubSubState.BadAuth;
                logger.WriteError("Unable to listen to topic: " + e.Topic + ", " + e.Response.Error);
            }
            else
            {
                pubsubState = PubSubState.OK;
                logger.WriteDebug("PubSub Topic " + e.Topic + " OK");
            }
        }

        private void Pubsub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(pubsubToken))
                {
                    return;
                }

                pubsubState = PubSubState.Connected;
                pubsub.SendTopics(pubsubToken);
                logger.WriteDebug("PubSub Service Connected");
            }
            catch (Exception exc)
            {
                pubsubToken = null;
                logger.WriteError(exc.ToString());
            }
        }

        private void Unsubscribe()
        {
            client.OnChatCommandReceived -= OnCommandReceived;
            client.OnMessageReceived -= OnMessageReceived;
            client.OnConnected -= OnConnected;
            client.OnDisconnected -= OnDisconnected;
            client.OnUserJoined -= OnUserJoined;
            client.OnUserLeft -= OnUserLeft;
            client.OnGiftedSubscription -= OnGiftedSub;
            client.OnCommunitySubscription -= OnPrimeSub;
            client.OnNewSubscriber -= OnNewSub;
            client.OnReSubscriber -= OnReSub;
            client.OnRaidNotification -= OnRaidNotification;

            client.OnConnectionError -= OnConnectionError;
            pubsub.OnChannelPointsRewardRedeemed -= Pubsub_OnChannelPointsRewardRedeemed;
        }
    }

    public enum PubSubState
    {
        NotConnected,
        Connecting,
        Connected,
        Authenticating,
        OK,

        BadAuth,
        ConnectionFailed
    }
}