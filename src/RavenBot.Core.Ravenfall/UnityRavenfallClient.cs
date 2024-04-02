using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenBot.Core.Chat;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Models;
using Shinobytes.Core;

namespace RavenBot.Core.Ravenfall
{
    public class UnityRavenfallClient : IRavenfallClient, IDisposable
    {
        private readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IUserProvider playerProvider;
        private readonly IMessageBus messageBus;
        private readonly IGameClient client;
        private readonly IUserSettingsManager settingsManager;
        private ITimeoutHandle keepAliveHandle;

        public IRavenfallApi Api { get; }
        public UnityRavenfallClient(
            ILogger logger,
            IKernel kernel,
            IUserProvider playerProvider,
            IMessageBus messageBus,
            IGameClient client,
            IUserSettingsManager settingsManager)
        {
            this.kernel = kernel;
            this.settingsManager = settingsManager;
            this.logger = logger;
            this.playerProvider = playerProvider;
            this.messageBus = messageBus;

            messageBus.Subscribe<UserJoinedEvent>(nameof(UserJoinedEvent), OnUserJoined);
            messageBus.Subscribe<UserLeftEvent>(nameof(UserLeftEvent), OnUserLeft);
            messageBus.Subscribe<CheerBitsEvent>(nameof(CheerBitsEvent), OnUserCheer);
            messageBus.Subscribe<UserSubscriptionEvent>(nameof(UserSubscriptionEvent), OnUserSub);

            this.client = client;
            this.Api = new RavenfallApi(client, EnqueueRequest, null);
            this.client.Connected += Client_OnConnect;
            this.client.Subscribe("session", RegisterSession);
            this.client.Subscribe("pubsub_token", RegisterPubSubToken);
            this.client.Subscribe("message", SendResponseToTwitchChat);
            this.keepAliveHandle = this.kernel.SetTimeout(KeepAlive, 1000);
        }

        private void KeepAlive()
        {
            var isConnected = client.IsConnected;
            if (!client.IsConnected)
            {
                client.ProcessAsync(Settings.UNITY_SERVER_PORT);
                isConnected = true;
            }
            this.keepAliveHandle = this.kernel.SetTimeout(KeepAlive, isConnected ? 5000 : 2000);
        }
        public IRavenfallApi this[ICommand cmd] => Ref(cmd.CorrelationId);
        public IRavenfallApi this[string correlationid] => Ref(correlationid);
        public IRavenfallApi Ref(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId)) return Api;
            return new RavenfallApi(client, EnqueueRequest, correlationId);
        }

        private void RegisterPubSubToken(GameMessageResponse obj)
        {
            var userId = obj.Args[0];
            var username = obj.Args[1];
            var token = obj.Args[2];

            messageBus.Send("pubsub_token", userId + "," + token);
        }

        private void RegisterSession(GameMessageResponse obj)
        {
            Guid.TryParse(obj.Args[0].ToString(), out var sessionid);
            Guid.TryParse(obj.Args[1].ToString(), out var userId);
            DateTime.TryParse(obj.Args[2].ToString(), out var sessionStart);

            var token = obj.Args[3] as JToken;
            var userSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(token.ToString());

            //var userSettings = obj.Args[3];

            settingsManager.Set(userId, userSettings);

            var player = playerProvider.Get(userId);
            player.IsBroadcaster = true;

            messageBus.Send("ravenfall_session", new LocalGameSessionInfo
            {
                SessionId = sessionid,
                SessionStart = sessionStart,
                Settings = userSettings,
                Owner = player
            });
        }

        public Task<bool> ProcessAsync(int serverPort)
            => this.client.ProcessAsync(serverPort);

        public void Dispose()
        {
            if (keepAliveHandle != null)
                kernel.ClearTimeout(keepAliveHandle);
            this.client.Dispose();
            this.client.Connected -= Client_OnConnect;
        }

        private async void Client_OnConnect(object sender, EventArgs e)
        {
            while (requests.TryDequeue(out var request))
            {
                await this.client.SendAsync(request);
            }
        }
        private async void OnUserCheer(CheerBitsEvent obj) => await SendAsync("twitch_cheer", obj);
        private async void OnUserSub(UserSubscriptionEvent obj) => await SendAsync("twitch_sub", obj);
        private void OnUserLeft(UserLeftEvent obj) => logger.WriteMessage(obj.Name + " left the channel");
        private void OnUserJoined(UserJoinedEvent obj) => logger.WriteMessage(obj.Name + " joined the channel");

        private Task SendAsync(string type, object content)
        {
            return SendAsync(type, User.ServerRequest, content);
        }

        private Task SendAsync(string type, User sender, object content)
        {
            return SendAsync(new GameMessage
            {
                Identifier = type,
                Sender = sender,
                Content = JsonConvert.SerializeObject(content),
                CorrelationId = null
            });
        }

        private async Task SendAsync(GameMessage msg)
        {
            var request = JsonConvert.SerializeObject(msg);
            if (!this.client.IsConnected)
            {
                this.EnqueueRequest(request);
                return;
            }
            await this.client.SendAsync(request);
        }

        private void SendResponseToTwitchChat(GameMessageResponse obj)
        {
            this.messageBus.Send(MessageBus.Broadcast, obj);
        }

        private void EnqueueRequest(string request)
        {
            this.requests.Enqueue(request);
        }
    }

    //public class BroadcastMessage
    //{
    //    public string User { get; set; }
    //    public string Message { get; set; }
    //}
}