using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using RavenBot.Core;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Twitch;
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
        private readonly IUserRoleManager userRoleManager;
        private readonly IRavenfallClient ravenfall;
        private readonly IPlayerProvider playerProvider;
        private readonly ITwitchUserStore userStore;
        private readonly ITwitchMessageFormatter messageFormatter;
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
            IUserRoleManager userRoleManager,
            IRavenfallClient ravenfall,
            IPlayerProvider playerProvider,
            ITwitchMessageFormatter localizer,
            IMessageBus messageBus,
            ICommandProvider commandProvider,
            ICommandHandler commandHandler,
            IChannelProvider channelProvider,
            IConnectionCredentialsProvider credentialsProvider)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.userRoleManager = userRoleManager;
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

        private void OnUserLeft(object sender, OnUserLeftArgs e)
        {
            if (!e.Channel.Contains(this.channelProvider.Get(), StringComparison.InvariantCultureIgnoreCase))
                return;

            this.messageBus.Send(nameof(TwitchUserLeft), new TwitchUserLeft(e.Username));
        }

        private void OnUserJoined(object sender, OnUserJoinedArgs e)
        {
            if (!e.Channel.Contains(this.channelProvider.Get(), StringComparison.InvariantCultureIgnoreCase))
                return;

            this.messageBus.Send(nameof(TwitchUserJoined), new TwitchUserJoined(e.Username));
        }

        private void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Bits == 0) return;

            this.messageBus.Send(
                nameof(TwitchCheer),
                new TwitchCheer(
                    e.ChatMessage.Channel,
                    e.ChatMessage.Id,
                    e.ChatMessage.Username,
                    e.ChatMessage.DisplayName,
                    e.ChatMessage.IsModerator,
                    e.ChatMessage.IsSubscriber,
                    e.ChatMessage.IsVip,
                    e.ChatMessage.Bits)
            );

            this.Broadcast("", Localization.Twitch.THANK_YOU_BITS, e.ChatMessage.DisplayName, e.ChatMessage.Bits);
        }

        private async void OnCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            var uid = e.Command.ChatMessage.UserId;
            await commandHandler.HandleAsync(this, new TwitchCommand(e.Command, userRoleManager.IsAdministrator(uid), userRoleManager.IsModerator(uid)));
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            if (this.broadcastSubscription == null)
                this.broadcastSubscription = messageBus.Subscribe<IGameCommand>(MessageBus.Broadcast, Broadcast);

            client.Initialize(credentialsProvider.Get(), channelProvider.Get());

            isInitialized = true;
        }

        public void Broadcast(IGameCommand message)
        {
            Broadcast(message.Receiver, message.Format, message.Args);
        }

        public void Broadcast(string user, string format, params object[] args)
        {
            if (!this.client.IsConnected || string.IsNullOrWhiteSpace(format))
                return;

            var channel = this.channelProvider.Get();

            if (client.JoinedChannels.Count == 0)
                client.JoinChannel(channel);

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            if (!string.IsNullOrEmpty(user))
                msg = user + ", " + msg;

            client.SendMessage(channel, msg);
        }

        private void CreateTwitchClient()
        {
            pubsub = new TwitchPubSub();
            client = new TwitchClient(new WebSocketClient(new ClientOptions
            {
                ClientType = ClientType.Chat,
                MessagesAllowedInPeriod = 100
            }));
        }

        private void OnReSub(object sender, OnReSubscriberArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
                new TwitchSubscription(
                    e.Channel,
                    e.ReSubscriber.UserId,
                    e.ReSubscriber.Login,
                    e.ReSubscriber.DisplayName,
                    null,
                    e.ReSubscriber.IsModerator,
                    e.ReSubscriber.IsSubscriber,
                    e.ReSubscriber.Months,
                    false));

            this.Broadcast("", Localization.Twitch.THANK_YOU_RESUB, e.ReSubscriber.DisplayName);
        }

        private void OnNewSub(object sender, OnNewSubscriberArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
                new TwitchSubscription(
                    e.Channel,
                    e.Subscriber.UserId,
                    e.Subscriber.Login,
                    e.Subscriber.DisplayName,
                    null,
                    e.Subscriber.IsModerator,
                    e.Subscriber.IsSubscriber,
                    1, true));

            this.Broadcast("", Localization.Twitch.THANK_YOU_SUB, e.Subscriber.DisplayName);
        }

        private void OnPrimeSub(object sender, OnCommunitySubscriptionArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
                new TwitchSubscription(
                    e.Channel,
                    e.GiftedSubscription.UserId,
                    e.GiftedSubscription.Login,
                    e.GiftedSubscription.DisplayName,
                    null,
                    e.GiftedSubscription.IsModerator,
                    e.GiftedSubscription.IsSubscriber,
                    1, false));

            this.Broadcast("", Localization.Twitch.THANK_YOU_SUB, e.GiftedSubscription.DisplayName);
        }

        private void OnGiftedSub(object sender, OnGiftedSubscriptionArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
            new TwitchSubscription(
                e.Channel,
                e.GiftedSubscription.Id,
                e.GiftedSubscription.Login,
                e.GiftedSubscription.DisplayName,
                e.GiftedSubscription.MsgParamRecipientId,
                e.GiftedSubscription.IsModerator,
                e.GiftedSubscription.IsSubscriber,
                1,
                false));

            this.Broadcast("", Localization.Twitch.THANK_YOU_GIFT_SUB, e.GiftedSubscription.DisplayName);
        }

        private void OnDisconnected(object sender, OnDisconnectedEventArgs e)
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

        private void OnConnected(object sender, OnConnectedArgs e)
        {
            logger.WriteDebug("Connected to Twitch IRC Server");
            messageBus.Send("twitch", "");
        }

        private void OnReconnected(object sender, OnReconnectedEventArgs e)
        {
            logger.WriteDebug("Reconnected to Twitch IRC Server");
            messageBus.Send("twitch", "");
        }

        private void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            this.Broadcast("", Localization.Twitch.THANK_YOU_RAID, e.RaidNotification.DisplayName);
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

            pubsub.OnListenResponse += Pubsub_OnListenResponse;
            pubsub.OnPubSubServiceConnected += Pubsub_OnPubSubServiceConnected;
            pubsub.OnPubSubServiceError += Pubsub_OnPubSubServiceError;
            pubsub.OnPubSubServiceClosed += Pubsub_OnPubSubServiceClosed;
            pubsub.OnChannelPointsRewardRedeemed += Pubsub_OnChannelPointsRewardRedeemed;
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