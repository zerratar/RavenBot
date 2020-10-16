using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RavenBot.Core;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Twitch;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace RavenBot
{
    public class TwitchCommandListener : ICommandListener, IMessageBroadcaster
    {
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IMessageBus messageBus;
        private readonly StringDb strings;
        private readonly ICommandHandler commandHandler;
        private readonly IChannelProvider channelProvider;
        private readonly IConnectionCredentialsProvider credentialsProvider;
        private IMessageBusSubscription broadcastSubscription;
        private TwitchClient client;
        private bool isInitialized;
        private int reconnectDelay = 10000;
        private bool tryToReconnect = true;
        private bool disposed;

        private readonly object mutex = new object();
        private readonly HashSet<string> newSubAdded = new HashSet<string>();

        public TwitchCommandListener(
            ILogger logger,
            IKernel kernel,
            IMessageBus messageBus,
            ICommandHandler commandHandler,
            IChannelProvider channelProvider,
            IConnectionCredentialsProvider credentialsProvider)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.messageBus = messageBus;
            this.strings = new StringDb();
            this.strings.Load();

            this.commandHandler = commandHandler;
            this.channelProvider = channelProvider;
            this.credentialsProvider = credentialsProvider;
            this.CreateTwitchClient();
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
            //const string SpawnRaidBossReward = "a92e0fcc-4c31-4b8b-b521-9cf4972a8fb7";
            //if (e.ChatMessage.CustomRewardId == SpawnRaidBossReward)
            //{
            //    logger.WriteDebug("SPAWN RAID BOSSUUU!!");
            //}

            if (e.ChatMessage.Bits == 0) return;

            this.messageBus.Send(
                nameof(TwitchCheer),
                new TwitchCheer(
                    e.ChatMessage.Id,
                    e.ChatMessage.Username,
                    e.ChatMessage.DisplayName,
                    e.ChatMessage.Bits)
            );

            this.Broadcast($"Thank you {e.ChatMessage.DisplayName} for the {e.ChatMessage.Bits} bits!!! <3");
        }

        private async void OnCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            await commandHandler.HandleAsync(this, new TwitchCommand(e.Command));
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;

            if (this.broadcastSubscription == null)
                this.broadcastSubscription = messageBus.Subscribe<BroadcastMessage>(MessageBus.Broadcast, Broadcast);

            client.Initialize(credentialsProvider.Get(), channelProvider.Get());
            isInitialized = true;
        }
        public void Broadcast(BroadcastMessage message)
        {
            if (!string.IsNullOrEmpty(message.User))
            {
                Broadcast($"{message.User}, {message.Message}");
                return;
            }

            Broadcast(message.Message);
        }

        public void Send(string target, string message)
        {
            Broadcast(new BroadcastMessage { User = target, Message = message });
        }

        public void Broadcast(string message)
        {
            if (!this.client.IsConnected) return;
            var channel = this.channelProvider.Get();

            if (client.JoinedChannels.Count == 0)
                client.JoinChannel(channel);

            var localizableMessage = message;
            var target = string.Empty;
            if (HasTarget(message))
            {
                localizableMessage = message.Substring(message.IndexOf(' ') + 1);
                target = message.Split(',')[0];
            }

            message = LocalizeMessage(localizableMessage);

            if (string.IsNullOrEmpty(message))
            {
                // in case the streamer chose not to have a message in here. 
                // don't send it. we dont want "name, " messages being sent.
                return;
            }

            if (!string.IsNullOrEmpty(target))
            {
                message = target + ", " + message;
            }

            client.SendMessage(channel, message);
        }

        private string LocalizeMessage(string localizableMessage)
        {
            try
            {
                var formatKey = strings.GetFormatKey(localizableMessage);
                return strings.KeyFormat(formatKey, localizableMessage);
            }
            finally
            {
                strings.Save();
            }
        }

        private static bool HasTarget(string message)
        {
            return message.IndexOf(',') > 0 && !message.Split(',')[0].Contains(" ");
        }

        private void CreateTwitchClient()
        {
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
                    e.ReSubscriber.UserId,
                    e.ReSubscriber.Login,
                    e.ReSubscriber.DisplayName,
                    null,
                    e.ReSubscriber.Months,
                    false));

            this.Broadcast($"Thank you {e.ReSubscriber.DisplayName} for the resub!!! <3");
        }

        private void OnNewSub(object sender, OnNewSubscriberArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
                new TwitchSubscription(
                    e.Subscriber.UserId,
                    e.Subscriber.Login,
                    e.Subscriber.DisplayName,
                    null, 1, true));

            this.Broadcast($"Thank you {e.Subscriber.DisplayName} for the sub!!! <3");
        }

        private void OnPrimeSub(object sender, OnCommunitySubscriptionArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
                new TwitchSubscription(
                    e.GiftedSubscription.UserId,
                    e.GiftedSubscription.Login,
                    e.GiftedSubscription.DisplayName,
                    null, 1, false));

            this.Broadcast($"Thank you {e.GiftedSubscription.DisplayName} for the sub!!! <3");
        }

        private void OnGiftedSub(object sender, OnGiftedSubscriptionArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
            new TwitchSubscription(
                e.GiftedSubscription.Id,
                e.GiftedSubscription.Login,
                e.GiftedSubscription.DisplayName,
                e.GiftedSubscription.MsgParamRecipientId,
                1,
                false));

            this.Broadcast($"Thank you {e.GiftedSubscription.DisplayName} for the gifted sub!!! <3");
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
            this.Broadcast($"Thank you {e.RaidNotification.DisplayName} for the raid! <3");
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
        }
    }
}