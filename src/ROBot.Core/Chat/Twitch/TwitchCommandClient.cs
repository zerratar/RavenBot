using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using TwitchLib.Client.Models;
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
        private bool rateLimited;
        private readonly List<string> rateLimitedChannels = new List<string>();

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

            //this.messageBus.Subscribe<PubSubToken>("pubsub", OnPubSubTokenReceived);
            broadcastSubscription = messageBus.Subscribe<SessionGameMessageResponse>(MessageBus.Broadcast, BroadcastAsync);
        }

        public string GetPubSubActivationLink()
        {
            return pubSubManager.GetActivationLink();
        }

        //public PubSubState GetPubSubState(ICommandChannel channel)
        //{
        //    var client = pubSubManager.GetPubSubClient(channel.Name);
        //    if (client == null)
        //    {
        //        return PubSubState.Disconnected;
        //    }

        //    return client.State;
        //}

        /*
         * Object Logic
         */

        private void Subscribe()
        {
            /* TwitchLib.Client Events */
            //Twitch Connection events            
            client.OnConnected += OnConnectedAsync;
            client.OnConnectionError += OnConnectionErrorAsync;
            client.OnDisconnected += OnDisconnectedAsync;
            client.OnIncorrectLogin += Client_OnIncorrectLogin;
            //in channel events

            client.OnChatCommandReceived += OnCommandReceivedAsync;
            client.OnMessageReceived += OnMessageReceivedAsync;
            client.OnUserJoined += OnUserJoinedAsync;
            client.OnUserLeft += OnUserLeftAsync;
            client.OnGiftedSubscription += OnGiftedSubAsync;
            client.OnCommunitySubscription += OnPrimeSubAsync;
            client.OnNewSubscriber += OnNewSubAsync;
            client.OnReSubscriber += OnReSubAsync;
            client.OnRaidNotification += OnRaidNotificationAsync;
            //Confirmation Events
            client.OnFailureToReceiveJoinConfirmation += OnFailureToReceiveJoinConfirmationAsync;
            client.OnJoinedChannel += OnJoinedChannelAsync;
            client.OnLeftChannel += OnLeftChannelAsync;
            client.OnMessageSent += OnMessageSentAsync; //When Twitch message sent (responded to sent messages with "USERSTATE" )
            client.OnUserStateChanged += OnUserStateChangedAsync;
            //Rate limited Events
            client.OnRateLimit += OnRateLimitAsync;
            //full events
            client.OnError += OnErrorAsync;
            //client.OnLog += OnLog;

            /* TwitchLib.PubSub Events */
            pubSubManager.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        }

        private async Task Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
        {
            logger.LogError("Failed to connect to Twitch IRC Server. Authentication Error: " + e.Exception);
        }

        private void Unsubscribe()
        {
            if (client != null)
            {
                //TwitchLib.Client
                client.OnChatCommandReceived -= OnCommandReceivedAsync;
                client.OnMessageReceived -= OnMessageReceivedAsync;
                client.OnConnected -= OnConnectedAsync;
                client.OnDisconnected -= OnDisconnectedAsync;
                client.OnIncorrectLogin -= Client_OnIncorrectLogin;
                client.OnUserJoined -= OnUserJoinedAsync;
                client.OnUserLeft -= OnUserLeftAsync;
                client.OnGiftedSubscription -= OnGiftedSubAsync;
                client.OnCommunitySubscription -= OnPrimeSubAsync;
                client.OnNewSubscriber -= OnNewSubAsync;
                client.OnReSubscriber -= OnReSubAsync;
                client.OnRaidNotification -= OnRaidNotificationAsync;
                client.OnFailureToReceiveJoinConfirmation -= OnFailureToReceiveJoinConfirmationAsync;
                client.OnJoinedChannel -= OnJoinedChannelAsync;
                client.OnLeftChannel -= OnLeftChannelAsync;
                client.OnConnectionError -= OnConnectionErrorAsync;
                client.OnMessageSent -= OnMessageSentAsync;
                client.OnUserStateChanged -= OnUserStateChangedAsync;
                client.OnRateLimit -= OnRateLimitAsync;
            }

            if (pubSubManager != null)
            {
                //TwitchLib.PubSub
                pubSubManager.OnChannelPointsRewardRedeemed -= OnChannelPointsRewardRedeemed;
            }
        }

        public string GetBotName()
        {
            var c = credentialsProvider.Get();
            if (c == null) return "RavenfallOfficial";
            return c.TwitchUsername;
        }

        public async Task StartAsync()
        {
            if (!kernel.Started) kernel.Start();

            // if we already have a client
            // clear previous event references to allow the
            // garbage collector to clean up the old client
            if (client != null)
            {
                Unsubscribe();
            }

            try
            {
                logger.LogInformation("[TWITCH] Starting...");

                client = new TwitchClient(new TcpClient(new ClientOptions(clientType: ClientType.Chat)));

                var credentials = credentialsProvider.Get();

                logger.LogInformation("[TWITCH] Initializing Client...");
                client.Initialize(credentials);

                Subscribe();

                logger.LogInformation("[TWITCH] Connecting...");
                await client.ConnectAsync();
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Unable to start Twitch Bot: " + exc);
            }
        }

        public async void Stop()
        {
            if (kernel.Started) kernel.Stop();
            if (client != null)
            {
                //client.OnLog -= OnLog;
                client.OnError -= OnErrorAsync;
            }
            Unsubscribe();

            allowReconnection = false;
            if (client.IsConnected)
                await client.DisconnectAsync();

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

        private async Task TryToReconnectAsync() //prepare to be broa...reconnected
        {
            if (attemptingReconnection)
                return; //Avoid more than one reconnect attempt. (I.e. disconnect fired twice)

            try
            {
                stats.ResetReceivedCount();

                attemptingReconnection = true;

                await ReconnectAsync();
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] " + nameof(TryToReconnectAsync) + " failed: " + exc);
            }
            finally
            {
                TestConnectionAsync();
            }
        }

        private async Task ReconnectAsync()
        {
            stats.AddTwitchAttempt();
            //client.Reconnect(); Abby: Seem to have a bug. Will Manually Do our own reconnect
            bool wasConnected = false;

            if (client != null && client.IsConnected)
            {
                wasConnected = true;
                //logger.LogError("[TWITCH] Recieved a Disconnect Event. Still connected, disconnecting");
                await client.DisconnectAsync(); //Thinks we're still connected after reciving Disconnection event, attempting to disconnect
            }

            attemptingReconnection = true;
            logger.LogWarning($"[TWITCH] Reconnecting (wasConnected: " + wasConnected + " Attempt: " + stats.TwitchConnectionCurrentAttempt + ")");

            if (stats.TwitchConnectionCurrentAttempt % 10 != 0)
            {
                await client.ConnectAsync(); //Rather than restarting the whole process, will just redo a connection
            }
            else
            {
                await StartAsync(); //Restart Process every 10 tries to see if this fix any connection issues
            }

        }
        private async Task TestConnectionAsync() //Start timer to test for connection
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
                    await StartAsync();
                    await TestConnectionAsync();
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

        private async Task RejoinChannelsAsync()
        {
            foreach (var c in joinedChannels)
            {
                await JoinChannelAsync(c);
            }

            while (channelJoinQueue.TryDequeue(out var channel))
            {
                await JoinChannelAsync(channel);
            }
        }
        public async Task JoinChannelAsync(string channel)
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
                    joinedChannels.Add(channel);

                    logger.LogDebug("[TWITCH] Joining Channel (Channel: " + channel + ")");
                    stats.AddChAttempt();
                    await client.JoinChannelAsync(channel);
                    //pubSubManager.PubSubConnect(channel);
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
        public async Task LeaveChannelAsync(string channel)
        {
            try
            {
                joinedChannels.Remove(channel);

                if (!InChannel(channel))
                {
                    return;
                }

                if (string.IsNullOrEmpty(channel))
                {
                    return;
                }

                pubSubManager.Disconnect(channel);
                await client.LeaveChannelAsync(channel);
            }
            catch (Exception exc)
            {
                logger.LogError("[TWITCH] Join Channel failed: " + exc);
            }
        }
        public bool InChannel(string channel)
        {
            return client.JoinedChannels.Any(x => x.Channel.ToLower() == channel.ToLower());
        }

        /*
         * Sending
         */
        public async Task BroadcastAsync(SessionGameMessageResponse cmd)
        {
            if (cmd == null || cmd.Session?.Name == null)
            {
                logger.LogError("Unable to broadcast message to " + cmd?.Message.Recipent.PlatformUserName);
                return;
            }

            var channel = cmd.Session.Channel;
            if (channel == null && !string.IsNullOrEmpty(cmd.Session.Name))
            {
                cmd.Session.Channel = channel = TryResolveChannel(cmd.Session.Name);
            }

            if (channel != null)
            {
                var message = cmd.Message;
                if (message.Recipent.Platform == "system")
                {
                    // system message, to ensure we dont get rate limited

                    if (rateLimited && rateLimitedChannels.Contains(channel.Name))
                    {
                        // wait 250ms before sending
                        await Task.Delay(250);
                    }

                    await SendMessageAsync(channel, message.Format, message.Args);
                    return;
                }

                if (message.Recipent.Platform == "twitch")
                {
                    if (rateLimited && rateLimitedChannels.Contains(channel.Name))
                    {
                        // wait 250ms before sending
                        await Task.Delay(250);
                    }
                    await SendReplyAsync(channel, message.Format, message.Args, message.CorrelationId, message.Recipent.PlatformUserName);
                }

                // ignore any platform that is not discord.
                // SendMessage(channel, message.Format, message.Args);
                //Broadcast(channel, message.Receiver, message.Format, message.Args);
            }
        }

        private ICommandChannel TryResolveChannel(string name)
        {
            var settings = settingsManager.GetAll();
            var s = settings.FirstOrDefault(x => x.TwitchUserName != null && x.TwitchUserName.ToLower() == name);
            if (s != null)
            {
                return new TwitchCommand.TwitchChannel(ulong.Parse(s.TwitchUserId), name);
            }

            return new TwitchCommand.TwitchChannel(name);
        }

        public async Task SendMessageAsync(ICommandChannel channel, string format, object[] args)
        {
            if (string.IsNullOrWhiteSpace(format))
                return;

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            await SendMessageAsync(channel, msg);
        }

        public async Task SendReplyAsync(ICommandChannel channel, string format, object[] args, string correlationId, string mention)
        {
            if (string.IsNullOrWhiteSpace(format))
                return;

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            await SendMessageAsync(channel, msg, correlationId, mention);
        }

        public async Task SendReplyAsync(ICommand command, string format, params object[] args)
        {
            await SendReplyAsync(command.Channel, format, args, command.CorrelationId, command.Mention);
        }

        public Task SendMessageAsync(ICommandChannel channel, string message)
        {
            return SendMessageAsync(channel, message, null, null);
        }

        public async Task SendMessageAsync(ICommandChannel channel, string message, string correlationId, string mention)
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
                await JoinChannelAsync(channelName);
                return;
            }

            // Process the chat message a final time before sending it off.
            message = await ApplyMessageTransformationAsync(channel, message);
            logger.LogDebug($"[TWITCH] Sending Message (Channel: {channel.Name} Message: {message})");
            stats.AddMsgSend(channel.Name, message);

            if (string.IsNullOrEmpty(correlationId))
            {
                if (!string.IsNullOrEmpty(mention))
                {
                    if (!mention.StartsWith("@"))
                    {
                        mention = "@" + mention;
                    }

                    message = mention + ", " + message;
                }

                await client.SendMessageAsync(channel.Name, message);
                return;
            }

            await client.SendReplyAsync(channel.Name, correlationId, message);
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

        private async Task OnUserLeftAsync(object sender, OnUserLeftArgs e)
        {
        }

        private async Task OnUserJoinedAsync(object sender, OnUserJoinedArgs e)
        {
        }

        private async Task OnMessageReceivedAsync(object sender, OnMessageReceivedArgs e)
        {
            if (await commandHandler.HandleAsync(game, this, e.ChatMessage))
            {
                stats.AddMsgRFCmdReceivedCount();
            }
        }

        private async Task OnCommandReceivedAsync(object sender, OnChatCommandReceivedArgs e)
        {
            if (e == null || e.Command == null)
            {
                logger.LogError("[TWITCH] OnCommandReceived: Received a null command. ???");
                return;
            }

            //if (!string.IsNullOrEmpty(e.Command.CommandText) && e.Command.CommandText.Equals("pubsub"))
            //{
            //    if (!e.Command.ChatMessage.IsBroadcaster)
            //    {
            //        return;
            //    }
            //    if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
            //    {
            //        return;
            //    }
            //}

            if (await commandHandler.HandleAsync(game, this, e.Command, e.ChatMessage))
            {
                stats.AddRFCommandCount();
            }
        }

        private async void OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            //logger.LogDebug("[TWITCH] Reward Redeemed (Title: " + e.RewardRedeemed?.Redemption?.Reward?.Title + " Name: " + e.RewardRedeemed?.Redemption?.User?.Login + " Channel: " + e.ChannelId + ")");
            await commandHandler.HandleAsync(game, this, e);
        }

        private async Task OnReSubAsync(object sender, OnReSubscriberArgs e)
        {
            messageBus.Send(nameof(UserSubscriptionEvent),
                 new UserSubscriptionEvent(
                     "twitch", e.Channel, e.ReSubscriber.UserId, e.ReSubscriber.Login, e.ReSubscriber.DisplayName, null,
                     e.ReSubscriber.UserDetail.IsModerator, e.ReSubscriber.UserDetail.IsSubscriber, e.ReSubscriber.MsgParamCumulativeMonths, false));
        }

        private async Task OnNewSubAsync(object sender, OnNewSubscriberArgs e)
        {
            messageBus.Send(nameof(UserSubscriptionEvent),
               new UserSubscriptionEvent("twitch", e.Channel, e.Subscriber.UserId, e.Subscriber.Login, e.Subscriber.DisplayName,
               null, e.Subscriber.UserDetail.IsModerator, e.Subscriber.UserDetail.IsSubscriber, 1, true));
            //this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_SUB, e.Subscriber.DisplayName);
        }

        private async Task OnPrimeSubAsync(object sender, OnCommunitySubscriptionArgs e)
        {
            messageBus.Send(nameof(UserSubscriptionEvent),
                new UserSubscriptionEvent("twitch", e.Channel, e.GiftedSubscription.UserId, e.GiftedSubscription.Login,
                e.GiftedSubscription.DisplayName, null, e.GiftedSubscription.UserDetail.IsModerator, e.GiftedSubscription.UserDetail.IsSubscriber, 1, false));
            //this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_SUB, e.GiftedSubscription.DisplayName);
        }

        private async Task OnGiftedSubAsync(object sender, OnGiftedSubscriptionArgs e)
        {
            messageBus.Send(nameof(UserSubscriptionEvent),
               new UserSubscriptionEvent("twitch", e.Channel, e.GiftedSubscription.Id, e.GiftedSubscription.Login,
               e.GiftedSubscription.DisplayName, e.GiftedSubscription.MsgParamRecipientId, e.GiftedSubscription.UserDetail.IsModerator,
               e.GiftedSubscription.UserDetail.IsSubscriber, 1, false));

            //this.Broadcast(e.Channel, "", Localization.Twitch.THANK_YOU_GIFT_SUB, e.GiftedSubscription.DisplayName);
        }

        private async Task OnDisconnectedAsync(object sender, OnDisconnectedEventArgs e)
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

            await TryToReconnectAsync();
        }

        private async Task OnUserStateChangedAsync(object sender, OnUserStateChangedArgs e)
        {
            //logger.LogDebug("[TWITCH] Client_OnUserStateChanged: " + e.ToString());
        }

        private async Task OnMessageSentAsync(object sender, OnMessageSentArgs e)
        {
            // this is already being logged in SendChatMessage
            //logger.LogDebug("[TWITCH] OnMessageSent (To: " + e.SentMessage.Channel + " Message: '" + e.SentMessage.Message + "')");
            if (e.SentMessage == null)
                return;

            stats.AddMsgSent(e.SentMessage.Channel, e.SentMessage.Message);
        }

        private async Task OnRateLimitAsync(object sender, NoticeEventArgs e)
        {
            stats.AddLastRateLimit(e);
            logger.LogError("[TWITCH] RateLimited (Channel: " + e.Channel + ", Message: " + e.Message + ")");
            rateLimited = true;
            rateLimitedChannels.Add(e.Channel);
        }

        private async Task OnConnectedAsync(object sender, TwitchLib.Client.Events.OnConnectedEventArgs e)
        {
            logger.LogDebug("[TWITCH] Connected");
            isConnectedToTwitch = true;
            hasConnectionError = false;
            stats.AddTwitchSuccess();
            stats.ResetTwitchAttempt();
            await RejoinChannelsAsync();
        }

        private async Task OnFailureToReceiveJoinConfirmationAsync(object sender, OnFailureToReceiveJoinConfirmationArgs e)
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

            if (string.IsNullOrEmpty(err))
            {
                logger.LogWarning("[TWITCH] Failed To Get Join Confirmation Without Reported Errors (Channel: " + e.Exception.Channel + ")");
            }
            else
            {
                logger.LogWarning("[TWITCH] Failed To Get Join Confirmation (Channel: " + e.Exception.Channel + " Error: " + err + ")");
            }

            await Task.Delay(1000);
            JoinChannelAsync(e.Exception.Channel);
        }

        private async Task OnRaidNotificationAsync(object sender, OnRaidNotificationArgs e)
        {
        }

        //private void OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        //{
        //    if (e == null)
        //        return;
        //    if (e.Data.StartsWith("Received:"))
        //        stats.ReceivedLog(e);
        //    //logger.LogDebug("[TWITCH] onLog (Log: " + e.Data + ")");
        //}

        private async Task OnErrorAsync(object sender, OnErrorEventArgs e)
        {
            logger.LogError("[TWITCH] Error: " + e.Exception);
            stats.AddTwitchError(e);
        }

        private async Task OnConnectionErrorAsync(object sender, OnConnectionErrorArgs e)
        {
            logger.LogError("[TWITCH] Connection Error: " + e.Error.Message);
            stats.AddTwitchError(e);
            hasConnectionError = true;
            isConnectedToTwitch = false;

            //TODO: Certain errors may require differnet actions - (example bad certs)
        }

        private async Task OnLeftChannelAsync(object sender, OnLeftChannelArgs e)
        {
            this.messageBus.Send(nameof(ChannelStateChangedEvent), new ChannelStateChangedEvent("twitch", e.Channel, false, null));
            stats.LeftChannel(e.Channel, client.JoinedChannels);
            logger.LogWarning("[TWITCH] Left Channel: " + e.Channel);
            currentlyJoiningChannels.TryRemove(e.Channel, out _);
        }

        private async Task OnJoinedChannelAsync(object sender, OnJoinedChannelArgs e)
        {
            this.messageBus.Send(nameof(ChannelStateChangedEvent), new ChannelStateChangedEvent("twitch", e.Channel, true, null));
            stats.JoinedChannel(e.Channel, client.JoinedChannels);
            logger.LogInformation("[TWITCH] Joined Channel: " + e.Channel);
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

