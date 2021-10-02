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

        private readonly object channelMutex = new object();

        private TwitchClient client;
        private bool isInitialized;
        private int reconnectDelay = 10000;
        private bool allowReconnection = true;
        private bool disposed;
        private bool isConnectedToTwitch;
        private long usedCommandCount = 0;
        private long messageCount = 0;
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

            //// For the time being, pubsub will be disabled as it needs actual token for the person that wants to use it? worked in the old bot. but not here. Wutfacers
            //// ugh...
            //this.messageBus.Subscribe<string>("streamer_userid_acquired", userid =>
            //{
            //    ListenForChannelPoints(logger, userid);
            //});

            this.messageBus.Subscribe<PubSubToken>("pubsub", OnPubSubTokenReceived);

            CreateTwitchClient();
        }

        private void OnPubSubTokenReceived(PubSubToken obj)
        {
            lock (channelMutex)
            {
                if (this.joinedChannels.Contains(obj.UserName))
                {
                    pubSubManager.Connect(obj.UserName);
                    //new TwitchLib.Api.Services.FollowerService()
                }
            }
        }

        public void Start()
        {
            if (!kernel.Started) kernel.Start();
            EnsureInitialized();
            Subscribe();
            client.Connect();
            RejoinChannels();
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
                logger.LogDebug("[Twitch] Trying to join a channel without a name.");
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
                            logger.LogWarning("[Twitch] Retrying to join Twitch Channel " + channel);
                        }
                        else
                        {
                            logger.LogDebug("[Twitch] Joining Twitch Channel " + channel);
                        }
                        joinedChannels.Add(channel);
                    }

                    client.JoinChannel(channel);

                    pubSubManager.Connect(channel);
                }
                else
                {
                    EnqueueJoin(channel);
                }
            }
            catch (Exception exc)
            {
                logger.LogWarning("[Twitch] Failed to join channel: " + channel + ". Retrying in a bit.. " + exc);
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
                logger.LogDebug("[Twitch] Trying to leave a channel without a name.");
                return;
            }

            logger.LogDebug("[Twitch] Leaving Twitch Channel " + channel);

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
            logger.LogDebug("[Twitch] Channel Point Reward Redeemed: " + e.RewardRedeemed?.Redemption?.Reward?.Title + ", by " + e.RewardRedeemed?.Redemption?.User?.Login + ", at " + e.ChannelId);
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
            logger.LogError("[Twitch] Disconnected from the Twitch IRC Server");
            Reconnect();
            client.Reconnect();
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            if (this.broadcastSubscription == null)
                this.broadcastSubscription = messageBus.Subscribe<IGameSessionCommand>(MessageBus.Broadcast, Broadcast);

            var credentials = credentialsProvider.Get();
            client.Initialize(credentials);

            //var api = new TwitchLib.Api.TwitchAPI();
            //api.Settings.ClientId = credentials.TwitchClientID

            isInitialized = true;
        }

        public void Broadcast(IGameSessionCommand message)
        {
            Broadcast(message.Session.Name, message.Receiver, message.Format, message.Args);
        }

        public void Broadcast(string channel, string user, string format, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(format))
                return;

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            if (!string.IsNullOrEmpty(user))
                msg = user + ", " + msg;

            SendChatMessage(channel, msg);
        }


        private void CreateTwitchClient()
        {
            isInitialized = false;
            client = new TwitchClient(new TcpClient(new ClientOptions { ClientType = ClientType.Chat }), TwitchLib.Client.Enums.ClientProtocol.TCP);
            client.AutoReListenOnException = true;
        }

        private void Reconnect()
        {
            try
            {
                client.Connect();
                usedCommandCount = 0;
                messageCount = 0;
                //if (client != null && client.IsConnected)
                //    return;
                //Unsubscribe();
                //isInitialized = false;
                //CreateTwitchClient();
                //Start();
            }
            catch (Exception)
            {
                //logger.LogInformation($"Failed to reconnect to the Twitch IRC Server. Retry in {reconnectDelay}ms");
                //Task.Run(async () =>
                //{
                //    await Task.Delay(reconnectDelay);

                //    if (!tryToReconnect)
                //        return;

                //    TryToReconnect();
                //});
            }
        }

        private void OnConnected(object sender, OnConnectedArgs e)
        {
            logger.LogDebug("[Twitch] Connected to Twitch IRC Server");
            this.isConnectedToTwitch = true;

            RejoinChannels();

            while (channelJoinQueue.TryDequeue(out var channel))
            {
                JoinChannel(channel);
            }
        }

        private async void OnFailureToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs e)
        {
            var err = "";
            if (!string.IsNullOrEmpty(e.Exception.Details))
                err = " with error: " + e.Exception.Details;
            logger.LogError("[Twitch] Failed to join channel: " + e.Exception.Channel + err);
            await Task.Delay(1000);
            JoinChannel(e.Exception.Channel);
        }

        private void OnReconnected(object sender, OnReconnectedEventArgs e)
        {
            this.isConnectedToTwitch = true;
            logger.LogDebug("[Twitch] Reconnected to Twitch IRC Server");
        }

        private void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
        }

        private void Pubsub_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                logger.LogError("[Twitch] " + e.Response.Error);
            }
            else
            {
                logger.LogDebug("[Twitch] PubSub Listen OK");
            }
        }

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
            pubSubManager.OnChannelPointsRewardRedeemed += Pubsub_OnChannelPointsRewardRedeemed;
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            logger.LogError("[Twitch] Connection Error: " + e.Error.Message + " - Maybe time to refresh auth token?");

            // Maybe its time to request a new Access Token?
        }

        private void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            logger.LogWarning("[Twitch] Left channel: " + e.Channel);

            currentlyJoiningChannels.TryRemove(e.Channel, out _);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            logger.LogInformation("[Twitch] Joined " + e.Channel + "");

            if (chatMessageQueue.TryGetValue(e.Channel, out var queue))
            {
                if (queue.Count > 0)
                {
                    logger.LogInformation("[Twitch] Sending " + queue.Count + " queued chat messages to " + e.Channel);
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
            pubSubManager.OnChannelPointsRewardRedeemed -= Pubsub_OnChannelPointsRewardRedeemed;
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