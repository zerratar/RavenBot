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

        private readonly HashSet<string> joinedChannels = new HashSet<string>();
        private readonly HashSet<string> suspendedChannels = new HashSet<string>();

        private readonly object channelMutex = new object();

        private TwitchClient client;
        private bool isInitialized;
        private int reconnectDelay = 10000;
        private bool allowReconnection = true;
        private bool disposed;
        private bool isConnectedToTwitch;
        private long usedCommandCount = 0;
        private long messageCount = 0;
        private bool hasConnectionError = false;
        private long connectionErrorCount = 0;
        private long connectionAttemptCount = 0;
        private bool attemptingReconnection = false;

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

        public void Start()
        {
            if (!kernel.Started) kernel.Start();
            EnsureInitialized();
            Subscribe();
            client.Connect();
        }

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
                logger.LogError("[TWITCH] Trying to join a channel without a name.");
                return;
            }

            if (currentlyJoiningChannels.TryGetValue(channel, out var isJoining) && (DateTime.UtcNow - isJoining) <= TimeSpan.FromSeconds(15))
            {
                return;
            }

            try
            {
                if (WaitForConnection(5))
                {
                    currentlyJoiningChannels[channel] = DateTime.UtcNow;
                    lock (channelMutex)
                    {
                        if (joinedChannels.Contains(channel))
                        {
                            logger.LogDebug("[TWITCH] Retrying to Join Channel (Channel: " + channel + ")");
                        }
                        else
                        {
                            logger.LogDebug("[TWITCH] Joining Channel (Channel: " + channel + ")");
                            joinedChannels.Add(channel);
                        }
                    }

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
                logger.LogDebug("[TWITCH] Unable to leave without channel name.");
                return;
            }

            logger.LogDebug("[TWITCH] Leaving Channel (Channel: " + channel + ")");

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

        public long GetCommandCount()
        {
            return usedCommandCount;
        }

        public long GetMessageCount()
        {
            return messageCount;
        }
        private void OnUserLeft(object sender, OnUserLeftArgs e)
        {
        }

        private void OnUserJoined(object sender, OnUserJoinedArgs e)
        {
        }

        private async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (await commandHandler.HandleAsync(game, this, e.ChatMessage))
            {
                ++messageCount;
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
                ++usedCommandCount;
            }
        }

        private async void Pubsub_OnChannelPointsRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e)
        {
            logger.LogDebug("[TWITCH] Reward Redeemed (Title: " + e.RewardRedeemed?.Redemption?.Reward?.Title + " Name: " + e.RewardRedeemed?.Redemption?.User?.Login + " Channel: " + e.ChannelId + ")");
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
                logger.LogError("[TWITCH] Disconnected.");
            }

            TryToReconnect();
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            if (this.broadcastSubscription == null)
                this.broadcastSubscription = messageBus.Subscribe<IGameSessionCommand>(MessageBus.Broadcast, Broadcast);

            var credentials = credentialsProvider.Get();
            client.Initialize(credentials);

            isInitialized = true;
        }

        public void Broadcast(IGameSessionCommand message)
        {
            Broadcast(message.Session.Name, message.Receiver, message.Format, message.Args);
        }

        public void Broadcast(string channel, string user, string format, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                logger.LogWarning($"[TWITCH] Broadcast Ignored - Empty Message (Channel: {channel} User: {user}");
                return;
            }

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
            {
                logger.LogWarning($"[TWITCH] Broadcast Ignored - Message became empty after formatting (Channel: {channel} Format: '{format}' Args: '{string.Join(",", args)}')");
                return;
            }

            if (!string.IsNullOrEmpty(user))
                msg = user + ", " + msg;

            logger.LogDebug($"[TWITCH] Sending Message (Channel: {channel} Message: '{msg}')");
            SendChatMessage(channel, msg);
        }


        private void CreateTwitchClient()
        {
            isInitialized = false;
            client = new TwitchClient(new TcpClient(new ClientOptions { ClientType = ClientType.Chat }), TwitchLib.Client.Enums.ClientProtocol.TCP);
            client.AutoReListenOnException = true;
        }

        private void TryToReconnect() //prepare to be broa...reconnected
        {
            if (this.attemptingReconnection)
                return; //Avoid more than one reconnect attempt. (I.e. disconnect fired twice)

            try
            {
                usedCommandCount = 0;
                messageCount = 0;

                this.attemptingReconnection = true;
                this.connectionAttemptCount = 0;

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
            this.connectionAttemptCount++;
            //client.Reconnect(); Abby: Seem to have a bug. Will Manually Do our own reconnect

            if (client != null && client.IsConnected)
            {
                logger.LogError("[TWITCH] Recieved a Disconnect Event. Still connected, disconnecting");
                this.attemptingReconnection = true;
                client.Disconnect(); //Thinks we're still connected after reciving Disconnection event, attempting to disconnect
            }

            logger.LogWarning($"[TWITCH] Reconnecting (Attempt: " + this.connectionAttemptCount + ")");

            Unsubscribe();
            isInitialized = false;
            CreateTwitchClient();
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
                logger.LogError("[TWITCH] Failed To Start Reconnection Timer");
            }

        }

        private void OnConnected(object sender, OnConnectedArgs e)
        {
            logger.LogDebug("[TWITCH] Connected");
            this.isConnectedToTwitch = true;
            this.connectionErrorCount = 0;
            this.hasConnectionError = false;

            RejoinChannels();

        }

        private async void OnFailureToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs e)
        {
            var err = "";
            if (!string.IsNullOrEmpty(e.Exception.Details))
            {
                err = " with error: " + e.Exception.Details;
                if (e.Exception.Details.Contains("suspended"))
                {
                    this.suspendedChannels.Add(e.Exception.Channel);
                }
            }
            logger.LogError("[TWITCH] Failed To Join Channel (Channel: " + e.Exception.Channel + " Error: " + err + ")");


            await Task.Delay(1000);
            JoinChannel(e.Exception.Channel);
        }

        private void OnReconnected(object sender, OnReconnectedEventArgs e)
        {
            //This seems to get called regardless of actual connection to server

            //this.isConnectedToTwitch = true;
            //logger.LogDebug("[Twitch] Reconnected to Twitch IRC Server");
            //this.connectionErrorCount = 0;
            //this.hasConnectionError = false;
            //this.attemptingReconnection = false;

            //RejoinChannels();

        }

        private void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
        }

        //Abby: This damn code block had me running in circles. I will smite thee code block with the bonk stick and the fury of a thousand black holes!
        //private void Pubsub_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        //{
        //    if (!e.Successful)
        //    {
        //        if(e.Response.Error == "ERR_BADAUTH")
        //        {
        //            //alert user
        //            //remove record for auth
        //        }
        //        logger.LogError("[TWITCH] PubSub Listen Response Error (Error: " + e.Response.Error + ")");
        //    }
        //    else
        //    {
        //        logger.LogDebug("[TWITCH] PubSub Listen OK");
        //    }
        //}

        private void Subscribe()
        {
            client.OnChatCommandReceived += OnCommandReceived;
            client.OnMessageReceived += OnMessageReceived;
            client.OnConnected += OnConnected;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnReconnected += OnReconnected;
            client.OnDisconnected += OnDisconnected;
            client.OnUserJoined += OnUserJoined;
            client.OnUserLeft += OnUserLeft;
            client.OnGiftedSubscription += OnGiftedSub;
            client.OnCommunitySubscription += OnPrimeSub;
            client.OnNewSubscriber += OnNewSub;
            client.OnReSubscriber += OnReSub;
            client.OnRaidNotification += OnRaidNotification;
            client.OnFailureToReceiveJoinConfirmation += OnFailureToReceiveJoinConfirmation;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnLeftChannel += Client_OnLeftChannel;
            client.OnError += Client_OnError;
            client.OnLog += Client_OnLog;
            pubSubManager.OnChannelPointsRewardRedeemed += Pubsub_OnChannelPointsRewardRedeemed;
            pubSubManager.OnListenFailBadAuth += Pubsub_OnListenFailBadAuth;
        }

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            logger.LogDebug("[TWITCH] Connection Log (Log: " + e.Data + ")");
        }

        private void Client_OnError(object sender, OnErrorEventArgs e)
        {
            logger.LogError("[TWITCH] Connection Error (Error: " + e.ToString() + ")");
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            logger.LogError("[TWITCH] Connection Error (Error: " + e.Error.Message + ")");
            this.connectionErrorCount++;
            this.hasConnectionError = true;
            isConnectedToTwitch = false;
        }

        private void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            logger.LogWarning("[TWITCH] Left Channel (Channel: " + e.Channel + ")");

            currentlyJoiningChannels.TryRemove(e.Channel, out _);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            logger.LogInformation("[TWITCH] Joined (Channel: " + e.Channel + ")");

            if (chatMessageQueue.TryGetValue(e.Channel, out var queue))
            {
                if (queue.Count > 0)
                {
                    logger.LogInformation("[TWITCH] Queued Sending (Count: " + queue.Count + " Channel: " + e.Channel + ")");
                }

                while (queue.TryDequeue(out var msg))
                {
                    SendChatMessage(e.Channel, msg);
                }
            }

            currentlyJoiningChannels.TryRemove(e.Channel, out _);
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
            client.OnFailureToReceiveJoinConfirmation -= OnFailureToReceiveJoinConfirmation;
            client.OnJoinedChannel -= Client_OnJoinedChannel;
            client.OnLeftChannel -= Client_OnLeftChannel;
            client.OnConnectionError -= Client_OnConnectionError;
            client.OnError -= Client_OnError;
            client.OnLog -= Client_OnLog;
            pubSubManager.OnChannelPointsRewardRedeemed -= Pubsub_OnChannelPointsRewardRedeemed;
            pubSubManager.OnListenFailBadAuth -= Pubsub_OnListenFailBadAuth;
        }

        private void Pubsub_OnListenFailBadAuth(object sender, OnListenResponseArgs e)
        {
            TwitchPubSubClient client = (TwitchPubSubClient)sender;
            //Commented to prevent spam until the pubsub reconnection is fixed. 
            //this.client.SendWhisper(client.getChannel(), "PubSub failed due to bad auth. To allow the bot to detect when channel points are used, run \"!pubsub activate\" in channel again. Thanks!");
            logger.LogDebug("[TWITCH] Auth Failed - (TODO SEND WHISPER) Whisper sent (Name: " + client.getChannel() + ")");
        }

        public void Dispose()
        {
            if (disposed)
                return;
            Unsubscribe();
            Stop();
            disposed = true;
        }
    }
}