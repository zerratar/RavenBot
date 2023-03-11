using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenBot.Core.Chat;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Models;
using RavenBot.Core.Ravenfall.Requests;
using Shinobytes.Core;

namespace RavenBot.Core.Ravenfall
{
    public class UnityRavenfallClient : IRavenfallClient, IDisposable
    {
        private readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
        private readonly ILogger logger;
        private readonly IUserProvider playerProvider;
        private readonly IMessageBus messageBus;
        private readonly IGameClient client;

        public IRavenfallApi Api { get; }

        public UnityRavenfallClient(
            ILogger logger,
            IUserProvider playerProvider,
            IMessageBus messageBus,
            IGameClient client)
        {
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

            this.client.Subscribe("session", RegisterSessionOwner);
            this.client.Subscribe("pubsub_token", RegisterPubSubToken);
            this.client.Subscribe("message", SendResponseToTwitchChat);

        }

        public IRavenfallApi Reply(string correlationId)
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

        private void RegisterSessionOwner(GameMessageResponse obj)
        {
            if (string.IsNullOrEmpty(obj.Args[0]?.ToString()))
                return;

            var plr = playerProvider.Get(obj.Args[0]?.ToString(), obj.Args[1]?.ToString());
            plr.IsBroadcaster = true;

            messageBus.Send("streamer_userid_acquired", plr.PlatformId);
        }

        public Task<bool> ProcessAsync(int serverPort)
            => this.client.ProcessAsync(serverPort);

        public void Dispose()
        {
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