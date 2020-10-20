using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using RavenBot.Core;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Twitch;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace RavenBot
{
    public class TwitchBot : ITwitchBot, IMessageChat
    {
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly ITwitchMessageFormatter messageFormatter;
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

        public TwitchBot(
            ILogger logger,
            IKernel kernel,
            ITwitchMessageFormatter localizer,
            IMessageBus messageBus,
            ICommandHandler commandHandler,
            IChannelProvider channelProvider,
            IConnectionCredentialsProvider credentialsProvider)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.messageFormatter = localizer;
            this.messageBus = messageBus;
            this.strings = new StringDb();
            this.strings.Load();

            this.commandHandler = commandHandler;
            this.channelProvider = channelProvider;
            this.credentialsProvider = credentialsProvider;
            this.CreateTwitchClient();
            this.Cleanup();
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

            this.Broadcast("Thank you {displayName} for the {bits} bits!!! <3", e.ChatMessage.DisplayName, e.ChatMessage.Bits);
        }

        private async void OnCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            await commandHandler.HandleAsync(this, new TwitchCommand(e.Command));
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
            Broadcast(message.Destination, message.Format, message.Args);
        }

        public void Broadcast(string message)
        {
            this.Broadcast(null, null, message);
        }

        public void Send(string target, string message)
        {
            Broadcast(target, message);
        }

        public void Broadcast(string format, params object[] args)
        {
            Broadcast(null, format, args);
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

            this.Broadcast("Thank you {displayName} for the resub!!! <3", e.ReSubscriber.DisplayName);
        }

        private void OnNewSub(object sender, OnNewSubscriberArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
                new TwitchSubscription(
                    e.Subscriber.UserId,
                    e.Subscriber.Login,
                    e.Subscriber.DisplayName,
                    null, 1, true));

            this.Broadcast("Thank you {displayName} for the sub!!! <3", e.Subscriber.DisplayName);
        }

        private void OnPrimeSub(object sender, OnCommunitySubscriptionArgs e)
        {
            this.messageBus.Send(nameof(TwitchSubscription),
                new TwitchSubscription(
                    e.GiftedSubscription.UserId,
                    e.GiftedSubscription.Login,
                    e.GiftedSubscription.DisplayName,
                    null, 1, false));

            this.Broadcast("Thank you {displayName} for the sub!!! <3", e.GiftedSubscription.DisplayName);
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

            this.Broadcast("Thank you {displayName} for the gifted sub!!! <3", e.GiftedSubscription.DisplayName);
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
            this.Broadcast("Thank you {displayName} for the raid! <3", e.RaidNotification.DisplayName);
        }

        private void Cleanup()
        {
            try
            {
                var folder = GetAssemblyDirectory();
                if (System.IO.Directory.GetFiles(folder, "ravenfall*").Length > 0)
                    return;

                var settingFiles = System.IO.Directory.GetFiles(folder, "*.json");
                foreach (var f in settingFiles)
                {
                    if (f.Contains("settings") && System.IO.File.Exists(f))
                    {
                        System.IO.File.Delete(f);
                    }
                }
            }
            catch { }
        }

        private string GetAssemblyDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            return Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(codeBase).Path));
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