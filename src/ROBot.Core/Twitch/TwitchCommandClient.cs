using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Twitch;
using ROBot.Core.Extensions;
using ROBot.Core.GameServer;
using Shinobytes.Ravenfall.RavenNet.Core;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Twitch
{
    public class TwitchCommandClient : ITwitchCommandClient
    {
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IBotServer game;
        private readonly IMessageBus messageBus;
        private readonly ITwitchMessageFormatter messageFormatter;
        private readonly ITwitchCommandController commandHandler;
        private readonly ITwitchCredentialsProvider credentialsProvider;
        private readonly ITwitchPubSubManager pubSubManager;
        private IMessageBusSubscription broadcastSubscription;

        //private readonly ConcurrentQueue<Tuple<string, string>> chatMessageQueue
        //    = new ConcurrentQueue<Tuple<string, string>>();

        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> chatMessageQueue
                 = new ConcurrentDictionary<string, ConcurrentQueue<string>>();

        private readonly ConcurrentDictionary<string, DateTime> currentlyJoiningChannels
            = new ConcurrentDictionary<string, DateTime>();

        private readonly ConcurrentQueue<string> channelJoinQueue
            = new ConcurrentQueue<string>();

        private readonly object channelMutex = new object();
        private readonly HashSet<string> joinedChannels = new HashSet<string>();
        private readonly HashSet<string> suspendedChannels = new HashSet<string>();

        private TwitchClient client;
        private bool isInitialized;
        private int reconnectDelay = 10000;
        private bool allowReconnection = true;
        private bool disposed;
        private bool isConnectedToTwitch;
        //private long usedCommandCount = 0;
        //private long messageCount = 0;
        private bool hasConnectionError = false;
        //private long connectionCurrentErrorCount = 0;
        //private long connectionCurrentAttemptCount = 0;
        private bool attemptingReconnection = false;

        private TwitchStats stats = new TwitchStats();

        public TwitchCommandClient(
            ILogger logger,
            IKernel kernel,
            IBotServer game,
            IMessageBus messageBus,
            ITwitchMessageFormatter messageFormatter,
            ITwitchCommandController commandHandler,
            ITwitchCredentialsProvider credentialsProvider,
            ITwitchPubSubManager pubSubManager)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.game = game;
            this.messageBus = messageBus;
            this.messageFormatter = messageFormatter;
            this.commandHandler = commandHandler;
            this.credentialsProvider = credentialsProvider;
            this.pubSubManager = pubSubManager;

            this.messageBus.Subscribe<PubSubToken>("pubsub", OnPubSubTokenReceived);

            CreateTwitchClient();
        }

        /*
         * Object Logic
         */

        private void Subscribe()
        {
            /* TwitchLib.Client Events */
            //Twitch Connection events            
            client.OnConnected += OnConnected;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnReconnected += OnReconnected;
            client.OnDisconnected += OnDisconnected;
            //in channel events
            client.OnChatCommandReceived += OnCommandReceived;
            client.OnMessageReceived += OnMessageReceived;
            client.OnUserJoined += OnUserJoined;
            client.OnUserLeft += OnUserLeft;
            client.OnGiftedSubscription += OnGiftedSub;
            client.OnCommunitySubscription += OnPrimeSub;
            client.OnNewSubscriber += OnNewSub;
            client.OnReSubscriber += OnReSub;
            client.OnRaidNotification += OnRaidNotification;
            //Confirmation Events
            client.OnFailureToReceiveJoinConfirmation += OnFailureToReceiveJoinConfirmation;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnLeftChannel += Client_OnLeftChannel;
            client.OnMessageSent += Client_OnMessageSent; //When Twitch message sent (responded to sent messages with "USERSTATE" )
            client.OnUserStateChanged += Client_OnUserStateChanged;
            //Rate limited Events
            client.OnRateLimit += Client_OnRateLimit;

            /* TwitchLib.PubSub Events */
            pubSubManager.OnChannelPointsRewardRedeemed += Pubsub_OnChannelPointsRewardRedeemed;
            pubSubManager.OnListenFailBadAuth += Pubsub_OnListenFailBadAuth;
        }

        private void Unsubscribe()
        {
            //TwitchLib.Client
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
            client.OnFailureToReceiveJoinConfirmation -= OnFailureToReceiveJoinConfirmation;
            client.OnJoinedChannel -= Client_OnJoinedChannel; //bot successfully joined channel
            client.OnLeftChannel -= Client_OnLeftChannel; //bot left channel
            client.OnConnectionError -= Client_OnConnectionError;

            client.OnMessageSent -= Client_OnMessageSent;
            client.OnUserStateChanged -= Client_OnUserStateChanged;
            client.OnRateLimit -= Client_OnRateLimit;

            //TwitchLib.PubSub
            pubSubManager.OnChannelPointsRewardRedeemed -= Pubsub_OnChannelPointsRewardRedeemed;
            pubSubManager.OnListenFailBadAuth -= Pubsub_OnListenFailBadAuth;
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            if (this.broadcastSubscription == null)
                this.broadcastSubscription = messageBus.Subscribe<IGameSessionCommand>(MessageBus.Broadcast, Broadcast);

            var credentials = credentialsProvider.Get();
            client.Initialize(credentials);

            isInitialized = true;

            Subscribe();
        }

        private void CreateTwitchClient()
        {
            isInitialized = false;
            client = new TwitchClient(new TcpClient(new ClientOptions { ClientType = ClientType.Chat }), TwitchLib.Client.Enums.ClientProtocol.TCP)
            {
                AutoReListenOnException = true,
                OverrideBeingHostedCheck = true //Override if BeingHosted - https://swiftyspiffy.com/TwitchLib/Client/class_twitch_lib_1_1_client_1_1_twitch_client.html#a5705479689fa4c440e38d62b5d50660e
            };
            client.OnLog += Client_OnLog;
            client.OnError += Client_OnError;
        }

        public void Start()
        {
            if (!kernel.Started) kernel.Start();
            EnsureInitialized();
            if (client.IsConnected)
                client.Connect();
        }

        public void Stop()
        {
            if (kernel.Started) kernel.Stop();
            Unsubscribe();

            allowReconnection = false;
            if (client.IsConnected)
                client.Disconnect();

            pubSubManager.Dispose();

            broadcastSubscription?.Unsubscribe();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            Stop();
            client.OnLog -= Client_OnLog;
            client.OnError -= Client_OnError;
            disposed = true;
        }

        /*
         * Twitch Connection/Reconnect
         */

        private bool WaitForConnection(int seconds)
        {
            var retries = seconds * 10;
            for (var i = 0; i < retries; ++i)
            {
                if (isConnectedToTwitch || client.IsConnected)
                    return true;
                System.Threading.Thread.Sleep(100);
            }
            return isConnectedToTwitch || client.IsConnected;
        }

        private void TryToReconnect() //prepare to be broa...reconnected
        {
            if (this.attemptingReconnection)
                return; //Avoid more than one reconnect attempt. (I.e. disconnect fired twice)

            try
            {
                stats.ResetReceivedCount();

                this.attemptingReconnection = true;

                ReconnectAttempt();
                testingConnection();
            }
            catch (Exception)
            {
                testingConnection();

            }
        }

        private void ReconnectAttempt()
        {
            stats.AddTwitchAttempt();
            //client.Reconnect(); Abby: Seem to have a bug. Will Manually Do our own reconnect
            bool wasConnected = false;

            if (client != null && client.IsConnected)
            {
                wasConnected = true;
                //logger.LogError("[TWITCH] Recieved a Disconnect Event. Still connected, disconnecting");
                client.Disconnect(); //Thinks we're still connected after reciving Disconnection event, attempting to disconnect
            }

            this.attemptingReconnection = true;
            logger.LogWarning($"[TWITCH] Reconnecting (wasConnected: " + wasConnected + " Attempt: " + stats.TwitchConnectionCurrentAttempt + ")");

            //Unsubscribe();
            //isInitialized = false;
            //CreateTwitchClient();
            Start();
        }
        private void testingConnection() //Start timer to test for connection
        {
            try
            {


                Task.Run(async () =>
                {
                    await Task.Delay(reconnectDelay); //Should make it a variable delay, increasing from a small delay up to a cap. (1 minute?)

                    if (!allowReconnection)
                        return;


                    if (isConnectedToTwitch && !this.hasConnectionError)
                    {
                        this.attemptingReconnection = false; //We did it!
                    }
                    else
                    {
                        ReconnectAttempt();
                        testingConnection();
                    }
                });
            }
            catch (Exception)
            {
                //logger.LogError("[TWITCH] Failed To Start Reconnection Timer");
            }

        }

        /*
         * Twitch Channels Logic
         */

        private void RejoinChannels()
        {
            lock (channelMutex)
            {
                foreach (var c in joinedChannels)
                {
                    JoinChannel(c);
                }
            }

            while (channelJoinQueue.TryDequeue(out var channel))
            {
                JoinChannel(channel);
            }
        }
        public void JoinChannel(string channel)
        {
            if (suspendedChannels.Contains(channel))
            {
                return;
            }

            if (!isConnectedToTwitch)
            {
                EnqueueJoin(channel);
                return;
            }

            if (InChannel(channel))
            {
                return;
            }

            if (string.IsNullOrEmpty(channel))
            {
                //logger.LogError("[TWITCH] Trying to join a channel without a name.");
                return;
            }

            if (currentlyJoiningChannels.TryGetValue(channel, out var isJoining) && (DateTime.UtcNow - isJoining) <= TimeSpan.FromSeconds(25))
            {
                return;
            }

            try
            {
                if (WaitForConnection(5))
                {
                    currentlyJoiningChannels[channel] = DateTime.UtcNow;
                    bool newChannel;

                    lock (channelMutex)
                    {
                        newChannel = joinedChannels.Contains(channel);
                        if (newChannel)
                            joinedChannels.Add(channel);
                    }

                    logger.LogDebug("[TWITCH] Joining Channel (Channel: " + channel + " Rejoin: " + newChannel + ")");
                    client.JoinChannel(channel);
                    pubSubManager.PubSubConnect(channel);
                }
                else
                {
                    EnqueueJoin(channel);
                }
            }
            catch (Exception exc)
            {
                logger.LogWarning("[TWITCH] Failed to Join Channel, Retrying later. (Channel: " + channel + " Exception: " + exc + ")");
                EnqueueJoin(channel);
            }
        }
        private void EnqueueJoin(string channel)
        {
            channelJoinQueue.Enqueue(channel);
        }
        public void LeaveChannel(string channel)
        {
            lock (channelMutex)
            {
                joinedChannels.Remove(channel);
            }

            if (!InChannel(channel))
            {
                return;
            }

            if (string.IsNullOrEmpty(channel))
            {
                //logger.LogDebug("[TWITCH] Unable to leave without channel name.");
                return;
            }

            //logger.LogDebug("[TWITCH] Leaving Channel (Channel: " + channel + ")");

            pubSubManager.Disconnect(channel);
            client.LeaveChannel(channel);
        }
        public IReadOnlyList<TwitchLib.Client.Models.JoinedChannel> JoinedChannels()
        {
            return client.JoinedChannels;
        }
        public bool InChannel(string channel)
        {
            return client.JoinedChannels.Any(x => x.Channel.ToLower() == channel.ToLower());
        }

        /*
         * Sending
         */
        public void Broadcast(IGameSessionCommand message)
        {
            Broadcast(message.Session.Name, message.Receiver, message.Format, message.Args);
        }
        public void Broadcast(string channel, string user, string format, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                //logger.LogWarning($"[TWITCH] Broadcast Ignored - Empty Message (Channel: {channel} User: {user}");
                return;
            }

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
            {
                //logger.LogWarning($"[TWITCH] Broadcast Ignored - Message became empty after formatting (Channel: {channel} Format: '{format}' Args: '{string.Join(",", args)}')");
                return;
            }

            if (!string.IsNullOrEmpty(user))
                msg = user + ", " + msg;


            SendChatMessage(channel, msg);
        }
        public void SendChatMessage(string channel, string message)
        {
            if (!client.IsConnected)
            {
                EnqueueChatMessage(channel, message);
                //chatMessageQueue.Enqueue(new Tuple<string, string>(channel, message));
                return;
            }

            if (!InChannel(channel))
            {
                EnqueueChatMessage(channel, message);
                JoinChannel(channel);
                return;
            }

            logger.LogDebug($"[TWITCH] Sending Message (Channel: {channel} Message: '{message}')");
            stats.AddMsgSend(channel, message);
            client.SendMessage(channel, message);
        }
        private void EnqueueChatMessage(string channel, string message)
        {
            if (!chatMessageQueue.TryGetValue(channel, out var queue))
            {
                chatMessageQueue[channel] = (queue = new ConcurrentQueue<string>());
            }
            queue.Enqueue(message);
        }

        ////////////
        // EVENTS //
        ////////////

        private void OnUserLeft(object sender, OnUserLeftArgs e)
        {
        }

        private void OnPubSubTokenReceived(PubSubToken obj)
        {
            lock (channelMutex)
            {
                if (this.joinedChannels.Contains(obj.UserName))
                {
                    logger.LogInformation("[RVNFLL] pubsub Token recieved for " + obj.UserName);
                    pubSubManager.PubSubConnect(obj.UserName);
                }
            }
        }

        private void OnUserJoined(object sender, OnUserJoinedArgs e)
        {
        }

        private async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (await commandHandler.HandleAsync(game, this, e.ChatMessage))
            {
                stats.AddMsgRFCmdReceivedCount();
            }
        }

        private async void OnCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (e == null || e.Command == null)
            {
                logger.LogError("[TWITCH] OnCommandReceived: Received a null command. ???");
                return;
            }

            if (!string.IsNullOrEmpty(e.Command.CommandText) && e.Command.CommandText.Equals("pubsub"))
            {
                if (!e.Command.ChatMessage.IsBroadcaster)
                {
                    return;
                }

                if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
                {
                    return;
                }

                if (e.Command.ArgumentsAsString.Contains("activate", StringComparison.OrdinalIgnoreCase))
                {
                    if (pubSubManager.IsReady(e.Command.ChatMessage.Channel))
                    {
                        return;
                    }

                    var activationLink = pubSubManager.GetActivationLink(e.Command.ChatMessage.UserId, e.Command.ChatMessage.Username);
                    client.SendWhisper(e.Command.ChatMessage.Username,
                        "Please use the following link to activate the channel point rewards. " +
                        activationLink
                        );
                    return;
                }
            }

            if (await commandHandler.HandleAsync(game, this, e.Command))
            {
                stats.AddRFCommandCount();
            }
        }

        private async void Pubsub_OnChannelPointsRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e)
        {
            //logger.LogDebug("[TWITCH] Reward Redeemed (Title: " + e.RewardRedeemed?.Redemption?.Reward?.Title + " Name: " + e.RewardRedeemed?.Redemption?.User?.Login + " Channel: " + e.ChannelId + ")");
            await commandHandler.HandleAsync(game, this, e);
        }

        private void OnReSub(object sender, OnReSubscriberArgs e)
        {
            this.messageBus.Send(nameof(ROBot.Core.Twitch.TwitchSubscription),
                 new ROBot.Core.Twitch.TwitchSubscription(e.Channel, e.ReSubscriber.UserId, e.ReSubscriber.Login, e.ReSubscriber.DisplayName, null, e.ReSubscriber.IsModerator, e.ReSubscriber.IsSubscriber, e.ReSubscriber.Months, false));
            this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_RESUB, e.ReSubscriber.DisplayName);
        }

        private void OnNewSub(object sender, OnNewSubscriberArgs e)
        {
            this.messageBus.Send(nameof(ROBot.Core.Twitch.TwitchSubscription),
               new ROBot.Core.Twitch.TwitchSubscription(e.Channel, e.Subscriber.UserId, e.Subscriber.Login, e.Subscriber.DisplayName, null, e.Subscriber.IsModerator, e.Subscriber.IsSubscriber, 1, true));
            this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_SUB, e.Subscriber.DisplayName);
        }

        private void OnPrimeSub(object sender, OnCommunitySubscriptionArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
                new ROBot.Core.Twitch.TwitchSubscription(e.Channel, e.GiftedSubscription.UserId, e.GiftedSubscription.Login, e.GiftedSubscription.DisplayName, null, e.GiftedSubscription.IsModerator, e.GiftedSubscription.IsSubscriber, 1, false));
            this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_SUB, e.GiftedSubscription.DisplayName);
        }

        private void OnGiftedSub(object sender, OnGiftedSubscriptionArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
               new ROBot.Core.Twitch.TwitchSubscription(e.Channel, e.GiftedSubscription.Id, e.GiftedSubscription.Login, e.GiftedSubscription.DisplayName, e.GiftedSubscription.MsgParamRecipientId, e.GiftedSubscription.IsModerator, e.GiftedSubscription.IsSubscriber, 1, false));

            this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_GIFT_SUB, e.GiftedSubscription.DisplayName);
        }

        private void OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            stats.AddTwitchDisconnect();
            this.isConnectedToTwitch = false;
            if (!allowReconnection)
            {
                logger.LogDebug("[TWITCH] Disconnected. Not attempting to reconnect. Most likely shutting down.");
                return;
            }

            if (this.hasConnectionError)
            {
                logger.LogError("[TWITCH] Disconnected with errors.");
            }
            else
            {
                logger.LogWarning("[TWITCH] Disconnected.");
            }

            TryToReconnect();
        }

        private void Client_OnUserStateChanged(object sender, OnUserStateChangedArgs e)
        {
            logger.LogDebug("[TWITCH] Client_OnUserStateChanged: " + e.ToString());
        }

        private void Client_OnMessageSent(object sender, OnMessageSentArgs e)
        {
            logger.LogDebug("[TWITCH] OnMessageSent: " + e.SentMessage);
            if (e.SentMessage == null)
                return;

            stats.AddMsgSent(e.SentMessage.Channel, e.SentMessage.Message);
            logger.LogDebug("[TWITCH] OnMessageSent: " + e.ToString());
        }

        private void Client_OnRateLimit(object sender, OnRateLimitArgs e)
        {
            logger.LogError("[TWITCH] RateLimited (OnRateLimitArgs: " + e.ToString() + ")");
        }

        private void OnConnected(object sender, OnConnectedArgs e)
        {
            logger.LogDebug("[TWITCH] Connected");
            this.isConnectedToTwitch = true;
            this.hasConnectionError = false;
            stats.AddTwitchSuccess();
            stats.ResetTwitchAttempt();

            RejoinChannels();

        }

        private async void OnFailureToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs e)
        {
            stats.AddChDisconnect();
            var err = "";
            if (!string.IsNullOrEmpty(e.Exception.Details))
            {
                err = " with error: " + e.Exception.Details;
                if (e.Exception.Details.Contains("suspended"))
                {
                    this.suspendedChannels.Add(e.Exception.Channel);
                    logger.LogWarning("[TWITCH] Failed To Get Join Confirmation [Suspended] (Channel: " + e.Exception.Channel + " Error: " + err + ")");
                    return; //Let not bother to reconnect
                }
            }

            if (err == "")
            {

                logger.LogWarning("[TWITCH] Failed To Get Join Confirmation Without Reported Errors (Channel: " + e.Exception.Channel + ")");
            }
            else
            {
                logger.LogWarning("[TWITCH] Failed To Get Join Confirmation (Channel: " + e.Exception.Channel + " Error: " + err + ")");
            }

            await Task.Delay(1000);
            JoinChannel(e.Exception.Channel);
        }

        private void OnReconnected(object sender, OnReconnectedEventArgs e)
        {
        }

        private void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
        }

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            if (e == null)
                return;
            if (e.Data.StartsWith("Received:"))
                stats.ReceivedLog();
            //logger.LogDebug("[TWITCH] onLog (Log: " + e.Data + ")");
        }

        private void Client_OnError(object sender, OnErrorEventArgs e)
        {
            logger.LogError("[TWITCH] onError (Error: " + e.ToString() + ")");
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            logger.LogError("[TWITCH] OnConnectionError (Error: " + e.Error.Message + ")");
            stats.AddTwitchError();
            this.hasConnectionError = true;
            isConnectedToTwitch = false;
        }

        private void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            stats.LeftChannel(e.Channel);
            logger.LogWarning("[TWITCH] Left Channel (Channel: " + e.Channel + ")");

            currentlyJoiningChannels.TryRemove(e.Channel, out _);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            stats.JoinedChannel(e.Channel);
            logger.LogInformation("[TWITCH] Joined (Channel: " + e.Channel + ")");
            if (chatMessageQueue.TryGetValue(e.Channel, out var queue))
            {
                if (queue.Count > 0)
                {
                    //logger.LogInformation("[TWITCH] Queued Sending (Count: " + queue.Count + " Channel: " + e.Channel + ")");
                }

                while (queue.TryDequeue(out var msg))
                {
                    SendChatMessage(e.Channel, msg);
                }
            }

            currentlyJoiningChannels.TryRemove(e.Channel, out _);
        }

        private void Pubsub_OnListenFailBadAuth(object sender, OnListenResponseArgs e)
        {
            //TODO: Event doesn't reach here - I'm doing something wrong, or missing something in my knowledge of firing events.
            TwitchPubSubClient client = (TwitchPubSubClient)sender;
            //Commented to prevent spam until the pubsub reconnection is fixed. 
            //this.client.SendWhisper(client.getChannel(), "PubSub failed due to bad auth. To allow the bot to detect when channel points are used, run \"!pubsub activate\" in channel again. Thanks!");
            //logger.LogDebug("[TWITCH] Auth Failed - (TODO SEND WHISPER) Whisper sent (Name: " + client.getChannel() + ")");
        }

        /*
         * Statistics
         */
        public ulong GetCommandCount()
        {
            return stats.UserCommandCount;
        }
        public ulong GetMessageCount()
        {
            return stats.UserMsgCount;
        }

        /*
         * TwitchStats - hold statistics to/from. 
         */
        /// <summary>
        /// Hold Statistics to/from twitch and on the connections.
        /// </summary>
        private class TwitchStats
        {
            //Fields
            //set alert threshold
            //connectionGap
            //msgGap
            //avg connection time
            //avg msg times
            //

            //Twitch Connection Stats
            private ulong _twitchConnectionTotalErrorCount = 0;
            private ulong _twitchConnectionTotalAttempt = 0;
            private ulong _twitchConnectionTotalSuccess = 0;
            private ulong _twitchConnectionTotalDisconnect = 0;
            private ulong _twitchConnectionCurrentAttempt = 0;
            private ulong _twitchConnectionCurrentErrorCount = 0;
            private ulong _twitchConnectionReconnectCount = 0;

            //User Channel Connection Stats
            private ulong _userChConnectionTotalCount = 0;
            private ulong _userChConnectionTotalDisconnectCount = 0;
            private ulong _userChConnectionAttempt = 0;

            //Message Sent Stats
            private ulong msgSendCount = 0;
            private ulong msgSentCount = 0;
            private ConcurrentDictionary<object, DateTime> msgTimes = new ConcurrentDictionary<object, DateTime>();
            private ConcurrentQueue<TimeSpan> listMsgDelay = new ConcurrentQueue<TimeSpan>();

            //Recieved from User Stats
            private ulong _userRFCommandCount = 0;
            private ulong _userMsgRFCmdCount = 0;

            private ulong _userTotalRFCommandCount = 0;
            private ulong _userTotalRFMsgCount = 0;

            //Recieved Logs Stats (onLogs = event raised everytime TwitchLib.Clients logs something. Usually data from twitch.)
            private DateTime lastRecievedLog;
            private object msgTimesMutex;


            //sortedList of top 10 idle times
            //last 20 idleTimes

            //use events to alert for unusnual twitch stats, such as higher than normal avg times between sent messages and sent confirmation. 

            //Properties 
            public ulong UserCommandCount { get => _userRFCommandCount; }
            public ulong UserMsgCount { get => _userMsgRFCmdCount; }
            public ulong UserTotalCommandCount { get => _userTotalRFCommandCount; }
            public ulong UserTotalMsgCount { get => _userTotalRFMsgCount; }
            public ulong TwitchConnectionTotalErrorCount { get => _twitchConnectionTotalErrorCount; }
            public ulong TwitchConnectionTotalAttempt { get => _twitchConnectionTotalAttempt; }
            public ulong TwitchConnectionTotalSuccess { get => _twitchConnectionTotalSuccess; }
            public ulong TwitchConnectionTotalDisconnect { get => _twitchConnectionTotalDisconnect; }
            public ulong TwitchConnectionCurrentAttempt { get => _twitchConnectionCurrentAttempt; }
            public ulong TwitchConnectionCurrentErrorCount { get => _twitchConnectionCurrentErrorCount; }
            public ulong TwitchConnectionReconnectCount { get => _twitchConnectionReconnectCount; }
            public ulong UserChConnectionCount { get => _userChConnectionTotalCount; }
            public ulong UserChConnectionAttempt { get => _userChConnectionAttempt; }
            public ulong MsgSendCount { get => msgSendCount; }
            public ulong MsgSentCount { get => msgSentCount; }
            public ulong UserChConnectionTotalDisconnectCount { get => _userChConnectionTotalDisconnectCount; }

            //Constructor
            public TwitchStats()
            {
            }

            //Methods
            public void AddTwitchAttempt()
            {
                this._twitchConnectionCurrentAttempt++;
                this._twitchConnectionTotalAttempt++;
            }

            public void AddTwitchError()
            {
                this._twitchConnectionTotalErrorCount++;
                this._twitchConnectionCurrentErrorCount++;
            }

            public void AddTwitchSuccess()
            {
                this._twitchConnectionTotalSuccess++;
            }

            public void ResetTwitchAttempt()
            {
                this._twitchConnectionCurrentAttempt = 0;
                this._twitchConnectionCurrentErrorCount = 0;
            }

            public void ResetReceivedCount()
            {
                this._userMsgRFCmdCount = 0;
                this._userRFCommandCount = 0;
            }

            public void AddRFCommandCount()
            {
                this._userRFCommandCount++;
                this._userTotalRFCommandCount++;
            }

            public void AddMsgRFCmdReceivedCount()
            {
                this._userMsgRFCmdCount++;
                this._userTotalRFMsgCount++;
            }

            public void AddTwitchDisconnect()
            {
                this._twitchConnectionTotalDisconnect++;
            }

            public void AddChDisconnect()
            {
                throw new NotImplementedException();
            }

            public void ReceivedLog()
            {
                throw new NotImplementedException();
            }

            public void LeftChannel(string channel)
            {
                throw new NotImplementedException();
            }

            public void JoinedChannel(string channel)
            {
                throw new NotImplementedException();
            }

            public void AddMsgSend(string channel, string message)
            {
                msgTimes.TryAdd(GetObject(channel, message), DateTime.Now);
            }
            public void AddMsgSent(string channel, string message)
            {
                object thisObj = GetObject(channel, message);
                DateTime value;
                if (msgTimes.TryGetValue(thisObj, out value))
                {
                    TimeSpan msgDelay = DateTime.Now - value;
                    AddMsgDelay(msgDelay);
                    msgTimes.TryRemove(thisObj, out _);
                }

                checkOldMsg();
            }

            private async void checkOldMsg()
            {

                foreach (var items in msgTimes)
                {
                    //items.Value
                }
            }

            public TimeSpan avgMsgDelays()
            {
                return TimeSpanExtensions.Average(listMsgDelay.AsEnumerable());
            }

            private void AddMsgDelay(TimeSpan msgDelay)
            {
                if (listMsgDelay.Count == 100)
                    listMsgDelay.TryDequeue(out _);

                listMsgDelay.Enqueue(msgDelay);
            }

            private object GetObject(string channel, string message)
            {
                channel = channel.ToLower().Trim();
                message = message.ToLower().Trim();

                return channel + message;
            }
        }
        
    }
}

