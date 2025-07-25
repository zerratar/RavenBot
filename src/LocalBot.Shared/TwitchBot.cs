﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        private readonly Core.IAppSettings settings;
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
        private string twitchId;
        private readonly object mutex = new object();
        private readonly HashSet<string> newSubAdded = new HashSet<string>();
        private PubSubState pubsubState;
        private bool tryPubSubAuthWithOAuthToken;
        private LocalGameSessionInfo ravenfallSession;
        private ITimeoutHandle queueTimeoutHandle;
        private readonly object pubsubListenMutex = new object();
        private readonly HttpClient httpClient;

        private readonly ConcurrentQueue<UserSubscriptionEvent> subQueue = new ConcurrentQueue<UserSubscriptionEvent>();
        private readonly ConcurrentQueue<CheerBitsEvent> cheerBitsQueue = new ConcurrentQueue<CheerBitsEvent>();

        public TwitchBot(
            ILogger logger,
            Core.IAppSettings settings,
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
            this.settings = settings;
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

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true;
            httpClient = new HttpClient(handler);

            this.messageBus.Subscribe<string>("pubsub_token", data =>
            {
                File.WriteAllText("pubsub-data.dat", data);
                ListenToChannelPoints(logger, data);
            });

            this.messageBus.Subscribe<LocalGameSessionInfo>("ravenfall_session", session =>
            {
                string twitchId = "";
                if (session != null)
                {
                    this.ravenfallSession = session;
                    var settings = session.Settings;

                    if (settings != null)
                    {

                        if (settings.TryGetValue("twitch_id", out var v))
                            twitchId = v?.ToString() ?? string.Empty;

                        if (settings.TryGetValue("twitch_pubsub", out var ps))
                            pubsubToken = ps?.ToString() ?? string.Empty;
                    }
                    else
                    {
                        logger.WriteWarning("Ravenfall sent empty session settings.");
                        try
                        {
                            logger.WriteWarning("Data Received:\n" + JsonConvert.SerializeObject(session));
                        }
                        catch
                        {
                            // ignored
                        }
                    }


                    if (string.IsNullOrEmpty(twitchId))
                    {
                        twitchId = this.ravenfallSession.Owner.PlatformId;
                    }

                    // this should also be the broadccaster.

                    playerProvider.SetBroadcaster(twitchId, session.Owner.Username);
                }

                if (tryPubSubAuthWithOAuthToken)
                {
                    var credentials = credentialsProvider.Get();
                    ListenToChannelPoints(logger, twitchId);// data + "," + credentials.TwitchOAuth);
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

            this.queueTimeoutHandle = kernel.SetTimeout(HandleTwitchEventQueue, 30000);
        }

        private async void HandleTwitchEventQueue()
        {
            try
            {
                while (cheerBitsQueue.TryDequeue(out var evt))
                {
                    if (!await OnUserCheerImplAsync(evt, false))
                    {
                        cheerBitsQueue.Enqueue(evt);
                        break; // try again later
                    }
                }
            }
            catch { }

            try
            {
                while (subQueue.TryDequeue(out var evt))
                {
                    if (!await OnUserSubImplAsync(evt, false))
                    {
                        subQueue.Enqueue(evt);
                        break; // try again later
                    }
                }
            }
            catch { }

            this.queueTimeoutHandle = kernel.SetTimeout(HandleTwitchEventQueue, 30000);
        }


        public bool CanRecieveChannelPointRewards => pubsubState == PubSubState.OK;

        private void ListenToChannelPoints(ILogger logger, string data)
        {
            return;
            lock (pubsubListenMutex)
            {
                if (pubsubState == PubSubState.Connecting || pubsubState == PubSubState.OK || pubsubState == PubSubState.Authenticating)
                {
                    return;
                }

                pubsubState = PubSubState.Connecting;

                try
                {
                    if (data.Contains(","))
                    {
                        var d = data.Split(',');
                        pubsubToken = d[1];
                        twitchId = d[0];
                    }
                    else
                    {
                        //pubsubToken = data;
                        twitchId = data;
                    }
                    logger.WriteDebug("Connecting to PubSub...");
                    pubsub.ListenToChannelPoints(twitchId);
                    pubsub.Connect();
                }
                catch (Exception exc)
                {
                    pubsubState = PubSubState.ConnectionFailed;
                    logger.WriteError(exc.ToString());
                }
            }
        }

        public async Task StartAsync()
        {
            if (!kernel.Started) kernel.Start();
            EnsureInitialized();
            Subscribe();
            await client.ConnectAsync();
        }

        public void Dispose()
        {
            if (disposed) return;
            Stop();
            disposed = true;
        }

        private async Task OnUserLeftAsync(object sender, OnUserLeftArgs e)
        {
            if (!e.Channel.Contains(this.channelProvider.Get(), StringComparison.InvariantCultureIgnoreCase))
                return;

            this.messageBus.Send(nameof(UserLeftEvent), new UserLeftEvent(e.Username));
        }

        private async Task OnUserJoinedAsync(object sender, OnUserJoinedArgs e)
        {
            if (!e.Channel.Contains(this.channelProvider.Get(), StringComparison.InvariantCultureIgnoreCase))
                return;

            this.messageBus.Send(nameof(UserJoinedEvent), new UserJoinedEvent(e.Username));
        }

        private async Task OnMessageReceivedAsync(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Bits == 0)
            {
#warning TODO: chat messages and send them to the game.
                return;
            }
            var evt = new CheerBitsEvent(
                    "twitch",
                    e.ChatMessage.Channel,
                    e.ChatMessage.Id,
                    e.ChatMessage.Username,
                    e.ChatMessage.DisplayName,
                    e.ChatMessage.IsModerator,
                    e.ChatMessage.IsSubscriber,
                    e.ChatMessage.IsVip,
                    e.ChatMessage.Bits);
            this.messageBus.Send(nameof(CheerBitsEvent), evt);
            await this.AnnounceAsync(Localization.Twitch.THANK_YOU_BITS, e.ChatMessage.DisplayName, e.ChatMessage.Bits);
            await OnUserCheerImplAsync(evt, true);
        }

        private async Task OnCommandReceivedAsync(object sender, OnChatCommandReceivedArgs e)
        {
            var uid = e.Command.ChatMessage.UserId;
            var settings = userSettingsManager.Get(uid, "twitch");
            await commandHandler.HandleAsync(this, new TwitchCommand(e.Command, e.Command.ChatMessage, settings.IsAdministrator, settings.IsModerator));
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            if (this.broadcastSubscription == null)
                this.broadcastSubscription = messageBus.Subscribe<GameMessageResponse>(MessageBus.Broadcast, BroadcastAsync);

            var token = credentialsProvider.Get();
            var channel = channelProvider.Get();
            client.Initialize(token, channel);

            isInitialized = true;
        }

        public async Task BroadcastAsync(GameMessageResponse message)
        {
            if (message.Recipent.Platform == "system")
            {
                // system message
                await SendMessageAsync(message.Format, message.Args);
                return;
            }

            if (message.Recipent.Platform == "twitch")
            {
                await SendReplyAsync(message.Format, message.Args, message.CorrelationId, message.Recipent.PlatformUserName);

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

        public Task AnnounceAsync(string format, params object[] args)
        {
            return SendMessageAsync(format, args);
        }

        public Task SendReplyAsync(ICommand cmd, string message, params object[] args)
        {
            return SendReplyAsync(message, args, cmd.CorrelationId, cmd.Mention);
        }

        public async Task SendReplyAsync(string format, object[] args, string correlationId, string mention)
        {
            try
            {
                if (!this.client.IsConnected || string.IsNullOrWhiteSpace(format))
                    return;

                var channel = this.channelProvider.Get();
                var joinedChannels = client.JoinedChannels;
                if (joinedChannels.Count == 0)
                {
                    await client.JoinChannelAsync(channel, true);
                }

                var msg = messageFormatter.Format(format, args);
                if (string.IsNullOrEmpty(msg))
                    return;

                if (!string.IsNullOrEmpty(correlationId))
                {
                    await client.SendReplyAsync(channel, correlationId, msg);
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

                await SendMessageAsync(msg);
            }
            catch (Exception exc)
            {
                logger.WriteError($"Error sending following message: \"{format}\", args: {string.Join(", ", args.Select(x => "\"" + x + "\""))}, mention: {mention}, correlation id: {correlationId}, exception: {exc}");
            }
        }

        public async Task SendMessageAsync(string format, object[] args)
        {
            if (!this.client.IsConnected || string.IsNullOrWhiteSpace(format))
                return;

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            await SendMessageAsync(msg);
        }

        private async Task SendMessageAsync(string msg)
        {
            var channel = this.channelProvider.Get();
            if (client.JoinedChannels.Count == 0)
            {
                await client.JoinChannelAsync(channel, true);
                await Task.Delay(1000);
            }

            if (client.JoinedChannels.Count > 0)
            {
                var messages = MessageUtilities.SplitMessage(msg);
                foreach (var part in messages)
                {
                    client.SendMessage(channel, part);
                    if (messages.Count > 1)
                    {
                        await Task.Delay(250);
                    }
                }
            }
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

            client = new TwitchClient(new TcpClient(options));// //WebSocketClient(options));
        }

        private async Task OnReSubAsync(object sender, OnReSubscriberArgs e)
        {
            int.TryParse(e.ReSubscriber.MsgParamCumulativeMonths, out var months);
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
                    months,
                    false));

            //this.Broadcast("", Localization.Twitch.THANK_YOU_RESUB, e.ReSubscriber.DisplayName);
        }

        private async Task OnNewSubAsync(object sender, OnNewSubscriberArgs e)
        {
            var sub = new UserSubscriptionEvent(
                    "twitch",
                    e.Channel,
                    e.Subscriber.UserId,
                    e.Subscriber.Login,
                    e.Subscriber.DisplayName,
                    null,
                    e.Subscriber.IsModerator,
                    e.Subscriber.IsSubscriber,
                    1, true);
            this.messageBus.Send(nameof(UserSubscriptionEvent), sub);
            await OnUserSubImplAsync(sub, true);
            //this.Broadcast("", Localization.Twitch.THANK_YOU_SUB, e.Subscriber.DisplayName);
        }

        private async Task OnPrimeSubAsync(object sender, OnCommunitySubscriptionArgs e)
        {
            var sub = new UserSubscriptionEvent(
                    "twitch",
                    e.Channel,
                    e.GiftedSubscription.UserId,
                    e.GiftedSubscription.Login,
                    e.GiftedSubscription.DisplayName,
                    null,
                    e.GiftedSubscription.IsModerator,
                    e.GiftedSubscription.IsSubscriber,
                    1, false);

            this.messageBus.Send(nameof(UserSubscriptionEvent), sub);
            await OnUserSubImplAsync(sub, true);
            //this.Broadcast("", Localization.Twitch.THANK_YOU_SUB, e.GiftedSubscription.DisplayName);
        }

        private async Task OnGiftedSubAsync(object sender, OnGiftedSubscriptionArgs e)
        {
            var sub = new UserSubscriptionEvent("twitch",
                e.Channel,
                e.GiftedSubscription.Id,
                e.GiftedSubscription.Login,
                e.GiftedSubscription.DisplayName,
                e.GiftedSubscription.MsgParamRecipientId,
                e.GiftedSubscription.IsModerator,
                e.GiftedSubscription.IsSubscriber,
                1,
                false);
            this.messageBus.Send(nameof(UserSubscriptionEvent), sub);
            await OnUserSubImplAsync(sub, true);
            //this.Broadcast("", Localization.Twitch.THANK_YOU_GIFT_SUB, e.GiftedSubscription.DisplayName);
        }

        private async Task OnDisconnectedAsync(object sender, OnDisconnectedEventArgs e)
        {
            logger.WriteDebug("Disconnected from the Twitch IRC Server");
            TryToReconnect();
        }

        private async Task<bool> OnUserCheerImplAsync(CheerBitsEvent @event, bool addToQueueOnFailure)
        {
            try
            {
                var json = JsonConvert.SerializeObject(@event);
                var statsData = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
#if DEBUG
                using (var response = await httpClient.PostAsync("https://localhost:5001/api/robot/twitch-cheer", statsData))
#else
                using (var response = await httpClient.PostAsync("https://www.ravenfall.stream/api/robot/twitch-cheer", statsData))
#endif
                {
                    response.EnsureSuccessStatusCode();
                }

                return true;
            }
            catch
            {
                if (addToQueueOnFailure)
                {
                    cheerBitsQueue.Enqueue(@event);
                }
                return false;
            }
        }

        private async Task<bool> OnUserSubImplAsync(UserSubscriptionEvent @event, bool addToQueueOnFailure)
        {
            try
            {
                var json = JsonConvert.SerializeObject(@event);
                var statsData = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
#if DEBUG
                using (var response = await httpClient.PostAsync("https://localhost:5001/api/robot/twitch-sub", statsData))
#else
                using (var response = await httpClient.PostAsync("https://www.ravenfall.stream/api/robot/twitch-sub", statsData))
#endif
                {
                    response.EnsureSuccessStatusCode();
                }

                return true;
            }
            catch
            {
                if (addToQueueOnFailure)
                {
                    subQueue.Enqueue(@event);
                }
                return false;
            }
        }


        private void TryToReconnect()
        {
            try
            {
                Unsubscribe();
                isInitialized = false;
                CreateTwitchClient();
                StartAsync();
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
                client.DisconnectAsync();

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

        private async Task Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
        {
            logger.WriteError("Failed to connect to Twitch IRC Server. Authentication Error: " + e.Exception);
        }



        //private void OnReconnected(object sender, OnReconnectedEventArgs e)
        //{
        //    logger.WriteDebug("Reconnected to Twitch IRC Server");
        //    messageBus.Send("twitch", "");
        //}

        private async Task OnRaidNotificationAsync(object sender, OnRaidNotificationArgs e)
        {
            this.AnnounceAsync(Localization.Twitch.THANK_YOU_RAID, e.RaidNotification.DisplayName);
        }

        private void Subscribe()
        {
            if (settings.CommandIdentifier != null)
            {
                var cmdIdentifier = settings.CommandIdentifier.Value;
                client.ChatCommandIdentifiers.Clear();
                client.ChatCommandIdentifiers.Add(cmdIdentifier);
            }

            client.OnChannelStateChanged += OnChannelStateChanged;
            client.OnChatCommandReceived += OnCommandReceivedAsync;
            client.OnLeftChannel += OnLeftChannel;
            client.OnJoinedChannel += OnJoinedChannel;
            client.OnUserStateChanged += OnUserStateChanged;

            client.OnSendReceiveData += OnSendReceiveData;

            client.OnMessageReceived += OnMessageReceivedAsync;
            client.OnIncorrectLogin += Client_OnIncorrectLogin;
            client.OnConnected += OnConnectedAsync;
            client.OnReconnected += OnReconnectedAsync;
            client.OnDisconnected += OnDisconnectedAsync;
            client.OnUserJoined += OnUserJoinedAsync;
            client.OnUserLeft += OnUserLeftAsync;
            client.OnGiftedSubscription += OnGiftedSubAsync;
            client.OnCommunitySubscription += OnPrimeSubAsync;
            client.OnNewSubscriber += OnNewSubAsync;
            client.OnReSubscriber += OnReSubAsync;
            client.OnRaidNotification += OnRaidNotificationAsync;
            client.OnConnectionError += OnConnectionErrorAsync;
            pubsub.OnListenResponse += Pubsub_OnListenResponse;
            pubsub.OnPubSubServiceConnected += Pubsub_OnPubSubServiceConnected;
            pubsub.OnPubSubServiceError += Pubsub_OnPubSubServiceError;
            pubsub.OnPubSubServiceClosed += Pubsub_OnPubSubServiceClosed;
            pubsub.OnChannelPointsRewardRedeemed += Pubsub_OnChannelPointsRewardRedeemed;
        }

        private async Task OnReconnectedAsync(object sender, OnConnectedArgs e)
        {
            logger.WriteDebug("Reconnected to Twitch IRC Server");
            messageBus.Send("twitch", "");
        }

        private async Task OnConnectedAsync(object sender, OnConnectedArgs e)
        {
            logger.WriteDebug("Connected to Twitch IRC Server");
            messageBus.Send("twitch", "");
        }

        private async Task OnSendReceiveData(object sender, OnSendReceiveDataArgs e)
        {
            //logger.WriteDebug("[" + e.Direction + "] " + e.Data);
        }

        private async Task OnUserStateChanged(object sender, OnUserStateChangedArgs e)
        {
            //logger.WriteWarning("Bot user state changed: " + Newtonsoft.Json.JsonConvert.SerializeObject(e.UserState));
        }
        private async Task OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            this.messageBus.Send(nameof(ChannelStateChangedEvent), new ChannelStateChangedEvent("twitch", e.Channel, true, null));
        }
        private async Task OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            this.messageBus.Send(nameof(ChannelStateChangedEvent), new ChannelStateChangedEvent("twitch", e.Channel, false, null));
            //logger.WriteWarning("Bot left channel: " + e.Channel);
        }

        private async Task OnChannelStateChanged(object sender, OnChannelStateChangedArgs e)
        {
            //logger.WriteDebug("Channel state changed: " + e.Channel + ", state: " + e.ChannelState);
        }

        private async Task OnConnectionErrorAsync(object sender, OnConnectionErrorArgs e)
        {
            logger.WriteError("Error connecting to Twitch: " + e.Error?.Message);
        }

        private void Pubsub_OnPubSubServiceError(object sender, TwitchLib.PubSub.Events.OnPubSubServiceErrorArgs e)
        {
            if (pubsubState == PubSubState.BadAuth)
            {
                return;
            }

            //logger.WriteError("PubSub Service Error: " + e.Exception);
        }

        private void Pubsub_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            if (pubsubState == PubSubState.OK)
            {
                logger.WriteWarning("Disconnected from PubSub");
            }

            pubsubState = PubSubState.NotConnected;
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
            client.OnChannelStateChanged -= OnChannelStateChanged;
            client.OnChatCommandReceived -= OnCommandReceivedAsync;
            client.OnLeftChannel -= OnLeftChannel;
            client.OnJoinedChannel -= OnJoinedChannel;
            client.OnIncorrectLogin -= Client_OnIncorrectLogin;
            client.OnUserStateChanged -= OnUserStateChanged;
            client.OnSendReceiveData -= OnSendReceiveData;

            client.OnMessageReceived -= OnMessageReceivedAsync;
            client.OnConnected -= OnConnectedAsync;
            client.OnReconnected -= OnReconnectedAsync;
            client.OnDisconnected -= OnDisconnectedAsync;
            client.OnUserJoined -= OnUserJoinedAsync;
            client.OnUserLeft -= OnUserLeftAsync;
            client.OnGiftedSubscription -= OnGiftedSubAsync;
            client.OnCommunitySubscription -= OnPrimeSubAsync;
            client.OnNewSubscriber -= OnNewSubAsync;
            client.OnReSubscriber -= OnReSubAsync;
            client.OnRaidNotification -= OnRaidNotificationAsync;
            client.OnConnectionError -= OnConnectionErrorAsync;
            pubsub.OnListenResponse -= Pubsub_OnListenResponse;
            pubsub.OnPubSubServiceConnected -= Pubsub_OnPubSubServiceConnected;
            pubsub.OnPubSubServiceError -= Pubsub_OnPubSubServiceError;
            pubsub.OnPubSubServiceClosed -= Pubsub_OnPubSubServiceClosed;
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