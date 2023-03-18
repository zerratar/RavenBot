﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RavenBot.Core.Chat;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.Chat.Twitch.PubSub;
using ROBot.Core.GameServer;
using ROBot.Core.Stats;
using Shinobytes.Core;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Chat.Twitch
{
    public partial class TwitchCommandClient : ITwitchCommandClient
    {
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IBotServer game;
        private readonly IMessageBus messageBus;
        private readonly IUserSettingsManager settingsManager;
        private readonly RavenBot.Core.IChatMessageFormatter messageFormatter;
        private readonly RavenBot.Core.IChatMessageTransformer messageTransformer;
        private readonly ITwitchCommandController commandHandler;
        private readonly ITwitchCredentialsProvider credentialsProvider;
        private readonly ITwitchPubSubManager pubSubManager;
        private readonly ITwitchStats stats;

        private IMessageBusSubscription broadcastSubscription;

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

        public TwitchCommandClient(
            ILogger logger,
            IKernel kernel,
            IBotServer game,
            IMessageBus messageBus,
            IUserSettingsManager settingsManager,
            RavenBot.Core.IChatMessageFormatter messageFormatter,
            RavenBot.Core.IChatMessageTransformer messageTransformer,
            ITwitchCommandController commandHandler,
            ITwitchCredentialsProvider credentialsProvider,
            ITwitchPubSubManager pubSubManager,
            ITwitchStats twitchStats)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.game = game;
            this.messageBus = messageBus;
            this.settingsManager = settingsManager;
            this.messageFormatter = messageFormatter;
            this.messageTransformer = messageTransformer;
            this.commandHandler = commandHandler;
            this.credentialsProvider = credentialsProvider;
            this.pubSubManager = pubSubManager;
            stats = twitchStats;

            this.messageBus.Subscribe<PubSubToken>("pubsub", OnPubSubTokenReceived);
            broadcastSubscription = messageBus.Subscribe<SessionGameMessageResponse>(MessageBus.Broadcast, Broadcast);
        }

        /*
         * Object Logic
         */

        private void Subscribe()
        {
            /* TwitchLib.Client Events */
            //Twitch Connection events            
            client.OnConnected += OnConnected;
            client.OnConnectionError += OnConnectionError;
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
            //full events
            client.OnError += OnError;
            client.OnLog += OnLog;

            /* TwitchLib.PubSub Events */
            pubSubManager.OnChannelPointsRewardRedeemed += Pubsub_OnChannelPointsRewardRedeemed;
        }

        private void Unsubscribe()
        {

            if (client != null)
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
                client.OnJoinedChannel -= Client_OnJoinedChannel;
                client.OnLeftChannel -= Client_OnLeftChannel;
                client.OnConnectionError -= OnConnectionError;
                client.OnMessageSent -= Client_OnMessageSent;
                client.OnUserStateChanged -= Client_OnUserStateChanged;
                client.OnRateLimit -= Client_OnRateLimit;
            }

            if (pubSubManager != null)
            {
                //TwitchLib.PubSub
                pubSubManager.OnChannelPointsRewardRedeemed -= Pubsub_OnChannelPointsRewardRedeemed;
            }
        }


        public void Start()
        {
            if (!kernel.Started) kernel.Start();

            Unsubscribe(); //Abby: I don't understand why we're unsubscribing, hee
                           //Karl: It is to make sure we there are no references left dangling
                           //      Older version of .NET used to cause the client object to never
                           //      be garbage collected since there were references to a live object (this class's methods)
                           // one option is to never create a new client here
            try
            {
                client =
                    new TwitchClient(new WebSocketClient(new ClientOptions
                    {
                        ClientType = ClientType.Chat,
                        MessagesAllowedInPeriod = 750,
                        ThrottlingPeriod = TimeSpan.FromSeconds(30)
                    }));

                client.AutoReListenOnException = true;

                var credentials = credentialsProvider.Get();

                client.Initialize(credentials);

                Subscribe();

                client.Connect();
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Unable to start Twitch Bot: " + exc);
            }
        }

        public void Stop()
        {
            if (kernel.Started) kernel.Stop();
            if (client != null)
            {
                client.OnLog -= OnLog;
                client.OnError -= OnError;
            }
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
            if (attemptingReconnection)
                return; //Avoid more than one reconnect attempt. (I.e. disconnect fired twice)

            try
            {
                stats.ResetReceivedCount();

                attemptingReconnection = true;

                ReconnectAttempt();
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] " + nameof(TryToReconnect) + " failed: " + exc);
            }
            finally
            {
                TestingConnection();
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

            attemptingReconnection = true;
            logger.LogWarning($"[TWITCH] Reconnecting (wasConnected: " + wasConnected + " Attempt: " + stats.TwitchConnectionCurrentAttempt + ")");

            if (stats.TwitchConnectionCurrentAttempt % 10 != 0)
            {
                client.Connect(); //Rather than restarting the whole process, will just redo a connection
            }
            else
            {
                Start(); //Restart Process every 10 tries to see if this fix any connection issues
            }

        }
        private async void TestingConnection() //Start timer to test for connection
        {
            try
            {
                await Task.Delay(reconnectDelay); //Should make it a variable delay, increasing from a small delay up to a cap. (1 minute?)

                if (!allowReconnection)
                    return;

                if (isConnectedToTwitch && !hasConnectionError)
                {
                    attemptingReconnection = false; //We did it!
                }
                else
                {
                    ReconnectAttempt();
                    TestingConnection();
                }
            }
            catch (Exception)
            {
                logger.LogError("[TWITCH] Failed To Start Reconnection Timer");
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

            if (currentlyJoiningChannels.TryGetValue(channel, out var isJoining) && DateTime.UtcNow - isJoining <= TimeSpan.FromSeconds(20))
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
                    stats.AddChAttempt();
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
        public bool InChannel(string channel)
        {
            return client.JoinedChannels.Any(x => x.Channel.ToLower() == channel.ToLower());
        }

        /*
         * Sending
         */
        public void Broadcast(SessionGameMessageResponse cmd)
        {
            if (cmd == null || cmd.Session?.Name == null)
            {
                logger.LogError("Unable to broadcast message to " + cmd?.Message.Recipent.PlatformUserName);
                return;
            }

            var channel = cmd.Session.Channel;
            if (channel == null)
            {
                // gotta find a channel.
                // ...
            }

            if (channel != null)
            {
                var message = cmd.Message;
                if (message.Recipent.Platform == "system")
                {
                    // system message
                    SendMessage(channel, message.Format, message.Args);
                    return;
                }

                if (message.Recipent.Platform == "twitch")
                {
                    if (!string.IsNullOrEmpty(message.CorrelationId))
                    {
                        SendReply(channel, message.Format, message.Args, message.CorrelationId);
                        return;
                    }

                    if (!string.IsNullOrEmpty(message.Recipent.PlatformUserName))
                    {
                        SendMessage(channel, message.Recipent.PlatformUserName + " " + message.Format, message.Args);
                    }
                    else
                    {
                        SendMessage(channel, message.Format, message.Args);
                    }
                }

                // ignore any platform that is not discord.
                // SendMessage(channel, message.Format, message.Args);
                //Broadcast(channel, message.Receiver, message.Format, message.Args);
            }
        }
        public async void SendMessage(ICommandChannel channel, string format, object[] args)
        {
            if (string.IsNullOrWhiteSpace(format))
                return;

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            await SendMessageAsync(channel, msg);
        }

        public async void SendReply(ICommandChannel channel, string format, object[] args, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(format))
                return;

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            await SendMessageAsync(channel, msg, correlationId);
        }

        public void SendReply(ICommand command, string format, params object[] args)
        {
            SendReply(command.Channel, format, args, command.CorrelationId);
        }

        public Task SendMessageAsync(ICommandChannel channel, string message)
        {
            return SendMessageAsync(channel, message, null);
        }

        public async Task SendMessageAsync(ICommandChannel channel, string message, string correlationId)
        {
            if (!client.IsConnected)
            {
                EnqueueChatMessage(channel, message);
                //chatMessageQueue.Enqueue(new Tuple<string, string>(channel, message));
                return;
            }

            var channelName = channel.Name;
            if (!InChannel(channelName))
            {
                EnqueueChatMessage(channel, message);
                JoinChannel(channelName);
                return;
            }

            // Process the chat message a final time before sending it off.
            message = await ApplyMessageTransformationAsync(channel, message);
            logger.LogDebug($"[TWITCH] Sending Message (Channel: {channel.Name} Message: {message})");
            stats.AddMsgSend(channel.Name, message);

            if (string.IsNullOrEmpty(correlationId))
            {
                client.SendMessage(channel.Name, message);
                return;
            }

            client.SendReply(channel.Name, correlationId, message);
        }
        private async Task<string> ApplyMessageTransformationAsync(ICommandChannel channel, string message)
        {
            var session = game.GetSession(channel);
            if (session != null && session.RavenfallUserId != Guid.Empty)
            {
                var settings = settingsManager.Get(session.RavenfallUserId);
                var transform = settings.ChatMessageTransformation;

                if (transform == ChatMessageTransformation.TranslateAndPersonalize)
                {
                    message = await messageTransformer.TranslateAndPersonalizeAsync(message, settings.ChatBotLanguage);
                }
                else if (transform == ChatMessageTransformation.Translate)
                {
                    message = await messageTransformer.TranslateAsync(message, settings.ChatBotLanguage);
                }
                if (transform == ChatMessageTransformation.Personalize)
                {
                    message = await messageTransformer.PersonalizeAsync(message);
                }
                //logger.LogDebug($"[TWITCH] Sending Message (Channel: {channel} Message: {message} Language: {settings.ChatBotLanguage} Transformation: {transform})");
            }

            return message;
        }

        private void EnqueueChatMessage(ICommandChannel channel, string message)
        {
            if (!chatMessageQueue.TryGetValue(channel.Name, out var queue))
            {
                chatMessageQueue[channel.Name] = queue = new ConcurrentQueue<string>();
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
                if (joinedChannels.Contains(obj.UserName))
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

        private async void Pubsub_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            //logger.LogDebug("[TWITCH] Reward Redeemed (Title: " + e.RewardRedeemed?.Redemption?.Reward?.Title + " Name: " + e.RewardRedeemed?.Redemption?.User?.Login + " Channel: " + e.ChannelId + ")");
            await commandHandler.HandleAsync(game, this, e);
        }

        private void OnReSub(object sender, OnReSubscriberArgs e)
        {
            messageBus.Send(nameof(UserSubscriptionEvent),
                 new UserSubscriptionEvent(
                     "twitch", e.Channel, e.ReSubscriber.UserId, e.ReSubscriber.Login, e.ReSubscriber.DisplayName, null,
                     e.ReSubscriber.IsModerator, e.ReSubscriber.IsSubscriber, e.ReSubscriber.Months, false));
        }

        private void OnNewSub(object sender, OnNewSubscriberArgs e)
        {
            messageBus.Send(nameof(UserSubscriptionEvent),
               new UserSubscriptionEvent("twitch", e.Channel, e.Subscriber.UserId, e.Subscriber.Login, e.Subscriber.DisplayName,
               null, e.Subscriber.IsModerator, e.Subscriber.IsSubscriber, 1, true));
            //this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_SUB, e.Subscriber.DisplayName);
        }

        private void OnPrimeSub(object sender, OnCommunitySubscriptionArgs e)
        {
            messageBus.Send(nameof(UserSubscriptionEvent),
                new UserSubscriptionEvent("twitch", e.Channel, e.GiftedSubscription.UserId, e.GiftedSubscription.Login,
                e.GiftedSubscription.DisplayName, null, e.GiftedSubscription.IsModerator, e.GiftedSubscription.IsSubscriber, 1, false));
            //this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_SUB, e.GiftedSubscription.DisplayName);
        }

        private void OnGiftedSub(object sender, OnGiftedSubscriptionArgs e)
        {
            messageBus.Send(nameof(UserSubscriptionEvent),
               new UserSubscriptionEvent("twitch", e.Channel, e.GiftedSubscription.Id, e.GiftedSubscription.Login,
               e.GiftedSubscription.DisplayName, e.GiftedSubscription.MsgParamRecipientId, e.GiftedSubscription.IsModerator,
               e.GiftedSubscription.IsSubscriber, 1, false));

            //this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_GIFT_SUB, e.GiftedSubscription.DisplayName);
        }

        private void OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            stats.AddTwitchDisconnect();
            isConnectedToTwitch = false;
            if (!allowReconnection)
            {
                logger.LogDebug("[TWITCH] Disconnected. Not attempting to reconnect. Most likely shutting down.");
                return;
            }

            if (hasConnectionError)
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
            //logger.LogDebug("[TWITCH] Client_OnUserStateChanged: " + e.ToString());
        }

        private void Client_OnMessageSent(object sender, OnMessageSentArgs e)
        {
            // this is already being logged in SendChatMessage
            //logger.LogDebug("[TWITCH] OnMessageSent (To: " + e.SentMessage.Channel + " Message: '" + e.SentMessage.Message + "')");
            if (e.SentMessage == null)
                return;

            stats.AddMsgSent(e.SentMessage.Channel, e.SentMessage.Message);
        }

        private void Client_OnRateLimit(object sender, OnRateLimitArgs e)
        {
            stats.AddLastRateLimit(e);
            logger.LogError("[TWITCH] RateLimited (OnRateLimitArgs: " + e.ToString() + ")");
        }

        private void OnConnected(object sender, OnConnectedArgs e)
        {
            logger.LogDebug("[TWITCH] Connected");
            isConnectedToTwitch = true;
            hasConnectionError = false;
            stats.AddTwitchSuccess();
            stats.ResetTwitchAttempt();
            RejoinChannels();
        }

        private async void OnFailureToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs e)
        {
            stats.AddChError();
            var err = "";
            if (!string.IsNullOrEmpty(e.Exception.Details))
            {
                err = " with error: " + e.Exception.Details;
                if (e.Exception.Details.Contains("suspended"))
                {
                    suspendedChannels.Add(e.Exception.Channel);
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

        private void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
        }

        private void OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            if (e == null)
                return;
            if (e.Data.StartsWith("Received:"))
                stats.ReceivedLog(e);
            //logger.LogDebug("[TWITCH] onLog (Log: " + e.Data + ")");
        }

        private void OnError(object sender, OnErrorEventArgs e)
        {
            logger.LogError("[TWITCH] onError (Error: " + e.ToString() + ")");
            stats.AddTwitchError(e);
        }

        private void OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            logger.LogError("[TWITCH] OnConnectionError (Error: " + e.Error.Message + ")");
            stats.AddTwitchError(e);
            hasConnectionError = true;
            isConnectedToTwitch = false;

            //TODO: Certain errors may require differnet actions - (example bad certs)
        }

        private void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            stats.LeftChannel(e.Channel, client.JoinedChannels);
            logger.LogWarning("[TWITCH] Left Channel (Channel: " + e.Channel + ")");

            currentlyJoiningChannels.TryRemove(e.Channel, out _);
        }

        private async void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            stats.JoinedChannel(e.Channel, client.JoinedChannels);
            logger.LogInformation("[TWITCH] Joined (Channel: " + e.Channel + ")");
            if (chatMessageQueue.TryGetValue(e.Channel, out var queue))
            {
                if (queue.Count > 0)
                {
                    //logger.LogInformation("[TWITCH] Queued Sending (Count: " + queue.Count + " Channel: " + e.Channel + ")");
                }

                while (queue.TryDequeue(out var msg))
                {
                    await SendMessageAsync(new TwitchCommand.TwitchChannel(e.Channel), msg);
                }
            }

            currentlyJoiningChannels.TryRemove(e.Channel, out _);
        }
    }
}
