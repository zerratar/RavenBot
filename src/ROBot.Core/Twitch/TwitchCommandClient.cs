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
        private IMessageBusSubscription broadcastSubscription;

        private readonly ConcurrentQueue<Tuple<string, string>> chatMessageQueue
            = new ConcurrentQueue<Tuple<string, string>>();

        private readonly ConcurrentQueue<string> channelJoinQueue
            = new ConcurrentQueue<string>();

        private readonly System.Collections.Generic.List<string> joinedChannels
            = new System.Collections.Generic.List<string>();

        private readonly object channelMutex = new object();

        private TwitchPubSub pubsub;
        private TwitchClient client;
        private bool isInitialized;
        private int reconnectDelay = 10000;
        private bool tryToReconnect = true;
        private bool disposed;

        private HashSet<string> connectedToPubsub = new HashSet<string>();
        private bool pubsubConnection;

        public TwitchCommandClient(
            ILogger logger,
            IKernel kernel,
            IBotServer game,
            IMessageBus messageBus,
            ITwitchMessageFormatter messageFormatter,
            ITwitchCommandController commandHandler,
            ITwitchCredentialsProvider credentialsProvider)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.game = game;
            this.messageBus = messageBus;
            this.messageFormatter = messageFormatter;
            this.commandHandler = commandHandler;
            this.credentialsProvider = credentialsProvider;

            // For the time being, pubsub will be disabled as it needs actual token for the person that wants to use it? worked in the old bot. but not here. Wutfacers
            // ugh...
            this.messageBus.Subscribe<string>("streamer_userid_acquired", userid =>
            {
                ListenForChannelPoints(logger, userid);
            });

            CreateTwitchClient();
        }

        private void ListenForChannelPoints(ILogger logger, string userid)
        {
            try
            {
                if (connectedToPubsub.Contains(userid))
                {
                    return;
                }

                if (pubsubConnection)
                {
                    pubsub.Disconnect();
                    pubsubConnection = false;
                }

                pubsub.ListenToChannelPoints(userid);
                pubsub.Connect();

                connectedToPubsub.Add(userid);
                logger.LogDebug("Connecting to PubSub");
            }
            catch (Exception exc)
            {
                logger.LogError(exc.ToString());
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

            tryToReconnect = false;
            if (client.IsConnected)
                client.Disconnect();

            try
            {
                pubsub.Disconnect();
            }
            catch
            {
            }
            pubsubConnection = false;
            broadcastSubscription?.Unsubscribe();
        }

        public void SendChatMessage(string channel, string message)
        {
            if (!client.IsConnected)
            {
                chatMessageQueue.Enqueue(new Tuple<string, string>(channel, message));
                return;
            }

            if (!InChannel(channel))
            {
                JoinChannel(channel);
            }

            client.SendMessage(channel, message);
        }

        public void JoinChannel(string channel)
        {
            if (InChannel(channel))
            {
                return;
            }

            if (string.IsNullOrEmpty(channel))
            {
                logger.LogDebug("Trying to join a channel without a name.");
                return;
            }

            try
            {
                if (WaitForConnection(5))
                {
                    client.JoinChannel(channel);
                    lock (channelMutex)
                    {
                        joinedChannels.Add(channel);
                    }
                }
                else
                {
                    EnqueueJoin(channel);
                }
            }
            catch
            {
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
                if (client.IsConnected)
                    return true;
                System.Threading.Thread.Sleep(100);
            }
            return client.IsConnected;
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
                logger.LogDebug("Trying to leave a channel without a name.");
                return;
            }

            client.LeaveChannel(channel);
        }

        private bool InChannel(string channel)
        {
            return client.JoinedChannels.Any(x => x.Channel.ToLower() == channel.ToLower());
        }

        private void OnUserLeft(object sender, OnUserLeftArgs e)
        {
        }

        private void OnUserJoined(object sender, OnUserJoinedArgs e)
        {
        }

        private async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            await commandHandler.HandleAsync(game, this, e.ChatMessage);
        }

        private async void OnCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            await commandHandler.HandleAsync(game, this, e.Command);
        }

        private async void Pubsub_OnChannelPointsRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e)
        {
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
            logger.LogInformation("Disconnected from the Twitch IRC Server");
            TryToReconnect();
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            if (this.broadcastSubscription == null)
                this.broadcastSubscription = messageBus.Subscribe<IGameSessionCommand>(MessageBus.Broadcast, Broadcast);

            client.Initialize(credentialsProvider.Get());
            isInitialized = true;
        }

        public void Broadcast(IGameSessionCommand message)
        {
            if (!connectedToPubsub.Contains(message.Session.UserId))
            {
                ListenForChannelPoints(logger, message.Session.UserId);
            }

            Broadcast(message.Session.Name, message.Receiver, message.Format, message.Args);
        }

        public void Broadcast(string channel, string user, string format, params object[] args)
        {
            if (!this.client.IsConnected || string.IsNullOrWhiteSpace(format))
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
            pubsub = new TwitchPubSub();
            client = new TwitchClient(new TcpClient(new ClientOptions { ClientType = ClientType.Chat }));
        }

        private void TryToReconnect()
        {
            try
            {
                if (client != null && client.IsConnected)
                    return;

                Unsubscribe();
                isInitialized = false;
                CreateTwitchClient();
                Start();
            }
            catch (Exception)
            {
                logger.LogInformation($"Failed to reconnect to the Twitch IRC Server. Retry in {reconnectDelay}ms");
                Task.Run(async () =>
                {
                    await Task.Delay(reconnectDelay);

                    if (!tryToReconnect)
                        return;

                    TryToReconnect();
                });
            }
        }

        private void OnConnected(object sender, OnConnectedArgs e)
        {
            logger.LogDebug("Connected to Twitch IRC Server");

            while (channelJoinQueue.TryDequeue(out var channel))
            {
                JoinChannel(channel);
            }

            while (chatMessageQueue.TryDequeue(out var msg))
            {
                SendChatMessage(msg.Item1, msg.Item2);
            }
        }

        private async void OnFailureToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs e)
        {
            await Task.Delay(1000);
            if (client.IsConnected)
            {
                JoinChannel(e.Exception.Channel);
            }
        }

        private void OnReconnected(object sender, OnReconnectedEventArgs e)
        {
            logger.LogDebug("Reconnected to Twitch IRC Server");
        }

        private void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
        }

        private void Pubsub_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                logger.LogError(e.Response.Error);
            }
            else
            {
                logger.LogDebug("PubSub Listen OK");
            }
        }

        private void Pubsub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            try
            {
                var credentials = credentialsProvider.Get();
                pubsub.SendTopics(credentials.TwitchOAuth);
                pubsubConnection = true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc.ToString());
            }
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
            client.OnFailureToReceiveJoinConfirmation += OnFailureToReceiveJoinConfirmation;

            pubsub.OnListenResponse += Pubsub_OnListenResponse;
            pubsub.OnPubSubServiceConnected += Pubsub_OnPubSubServiceConnected;
            pubsub.OnChannelPointsRewardRedeemed += Pubsub_OnChannelPointsRewardRedeemed;
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

            pubsub.OnListenResponse -= Pubsub_OnListenResponse;
            pubsub.OnPubSubServiceConnected -= Pubsub_OnPubSubServiceConnected;
            pubsub.OnChannelPointsRewardRedeemed -= Pubsub_OnChannelPointsRewardRedeemed;
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