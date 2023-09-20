using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenBot.Core.Chat;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Models;
using RavenBot.Core.Ravenfall.Requests;
using Shinobytes.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ROBot.Core.GameServer
{
    public class RavenfallConnection : IRavenfallConnection
    {
        private readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IBotServer server;
        private readonly IUserProvider playerProvider;
        private readonly IMessageBus messageBus;
        private readonly IUserSettingsManager settingsManager;
        private readonly RavenfallGameClientConnection client;
        private RemoteGameSessionInfo queuedSessionInfo;
        private IGameSession session;
        private ITimeoutHandle activePing;

        private IPEndPoint endPoint;

        private int pingSendIndex = 0;
        private int pongReceiveIndex = 0;
        private int missedPingCount = 0;
        private bool disposed;
        private readonly List<IMessageBusSubscription> subs = new List<IMessageBusSubscription>();

        public Guid InstanceId { get; } = Guid.NewGuid();

        public RavenfallConnection(
            ILogger logger,
            IKernel kernel,
            IBotServer server,
            IUserProvider playerProvider,
            IMessageBus messageBus,
            IUserSettingsManager settingsManager,
            RavenfallGameClientConnection client)
        {
            this.settingsManager = settingsManager;
            this.logger = logger;
            this.kernel = kernel;
            this.server = server;
            this.playerProvider = playerProvider;
            this.messageBus = messageBus;

            this.subs.Add(messageBus.Subscribe<UserJoinedEvent>(nameof(UserJoinedEvent), OnUserJoined));
            this.subs.Add(messageBus.Subscribe<UserLeftEvent>(nameof(UserLeftEvent), OnUserLeft));
            this.subs.Add(messageBus.Subscribe<CheerBitsEvent>(nameof(CheerBitsEvent), OnUserCheer));
            this.subs.Add(messageBus.Subscribe<UserSubscriptionEvent>(nameof(UserSubscriptionEvent), OnUserSub));

            this.client = client;

            this.Api = new RavenfallApi(client, EnqueueRequest, null);

            this.client.Connected += Client_Connected;
            this.client.Disconnected += Client_Disconnected;

            this.client.Subscribe("session", RegisterSession);
            this.client.Subscribe("pong", PongReceived);
            this.client.Subscribe("message", SendResponseToTwitchChat);

            if (this.client.IsConnected)
            {
                Client_Connected(this, EventArgs.Empty);
            }

        }
        public IRavenfallApi this[ICommand cmd] => Ref(cmd.CorrelationId);
        public IRavenfallApi this[string correlationid] => Ref(correlationid);
        public IRavenfallApi Ref(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId)) return Api;
            return new RavenfallApi(client, EnqueueRequest, correlationId);
        }

        private event EventHandler<RemoteGameSessionInfo> internalSessionInfoReceived;

        public event EventHandler<RemoteGameSessionInfo> OnSessionInfoReceived
        {
            add
            {
                internalSessionInfoReceived += value;
                //if (value != null && queuedSessionInfo != null)
                //{
                //    internalSessionInfoReceived.Invoke(this, queuedSessionInfo);
                //}
            }
            remove
            {
                internalSessionInfoReceived -= value;
            }
        }

        public event EventHandler<RemoteGameSessionInfo> OnSessionNameChanged;

        public IGameSession Session
        {
            get => session;
            set
            {
                session = value;
                client.Session = value;
            }
        }

        public IPEndPoint EndPoint
        {
            get
            {
                if (endPoint != null || client == null)
                {
                    return endPoint;
                }

                return client.EndPoint;
            }
        }

        public string EndPointString
        {
            get
            {
                try
                {
                    if (client == null)
                    {
                        return "Unknown";
                    }

                    return EndPoint != null ? EndPoint.Address + ":" + EndPoint.Port : "Unknown";
                }
                catch
                {
                    return "Unknown";
                }
            }
        }

        public IRavenfallApi Api { get; }

        private void PongReceived(GameMessageResponse obj)
        {
            int.TryParse(obj.CorrelationId, out pongReceiveIndex);
            missedPingCount = 0;
        }


        private void RegisterSession(GameMessageResponse obj)
        {
            try
            {
                // TODO: should not need to make it into string here, they will have the correct
                //       formats already, so its better to just do a normal cast.

                Guid.TryParse(obj.Args[0].ToString(), out var sessionid);
                Guid.TryParse(obj.Args[1].ToString(), out var userId);
                DateTime.TryParse(obj.Args[2].ToString(), out var sessionStart);

                var token = obj.Args[3] as JToken;
                var userSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(token.ToString());

                //var userSettings = obj.Args[3];

                settingsManager.Set(userId, userSettings);

                var player = playerProvider.Get(userId);
                player.IsBroadcaster = true;

                if (internalSessionInfoReceived == null)
                {
                    return;
                }
                var sessionInfo = new RemoteGameSessionInfo
                {
                    Created = sessionStart,
                    SessionId = sessionid,
                    UserId = userId,
                    Owner = player,
                    Settings = userSettings
                };

                internalSessionInfoReceived.Invoke(this, sessionInfo);

                messageBus.Send("ravenfall_session", sessionInfo);
            }
            catch (Exception exc)
            {
                logger.LogError($"RegisterSessionOwner session: {Session?.Name}, failed: " + exc);
            }
        }

        public void Dispose()
        {
            try
            {
                if (!this.disposed)
                {
                    if (subs.Count > 0)
                    {
                        subs.ForEach(x => x.Unsubscribe());
                    }

                    this.client.Connected -= Client_Connected;
                    this.client.Disconnected -= Client_Disconnected;
                    this.client.Dispose();
                    disposed = true;
                    return;
                }
            }
            catch (Exception exc)
            {
                logger.LogError("[RVNFLL] Failed to Dispose Connection: " + exc);
                return;
            }

            logger.LogError("[RVNFLL] Failed to Dispose Connection: Already Disposed");
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            Dispose();
            server.OnClientDisconnected(this);
            if (activePing != null)
            {
                kernel.ClearTimeout(activePing);
            }
        }

        private async void Client_Connected(object sender, EventArgs e)
        {
            //server.OnClientConnected(this);
            this.endPoint = this.client.EndPoint;
            //activePing = kernel.SetTimeout(PingPong, 15000);
            PingPong();

            while (requests.TryDequeue(out var request))
            {
                await this.client.SendAsync(request);
            }
        }

        private void PingPong()
        {
            if (pingSendIndex != pongReceiveIndex)
            {
                logger.LogDebug("[RVNFALL] Connection has not sent any pong back. since last update. Ping " + pingSendIndex + ", Pong " + pongReceiveIndex);
                // Do nothing as of for now. Since clients have not been updated.
                // But otherwise we should have a fail count
                missedPingCount++;
                // and if that goes beyond 2, client should be disconnected. So the game can force reconnect.
                if (missedPingCount > 2)
                {
                    // this.client.Close();
                    // return;
                }
            }

            if (activePing != null)
                kernel.ClearTimeout(activePing);

            Api.Ping(pingSendIndex++);

            activePing = kernel.SetTimeout(() => PingPong(), 3000);
        }

        private async void OnUserCheer(CheerBitsEvent obj)
        {
            if (session == null || !session.Name.Equals(obj.Channel, StringComparison.OrdinalIgnoreCase))
                return;

            logger.LogDebug("[TWITCH] Bits Cheered (Channel: " + obj.Channel + " Bits: " + obj.Bits + " From: " + obj.DisplayName + ")");
            await SendAsync("twitch_cheer", obj);
        }

        private async void OnUserSub(UserSubscriptionEvent obj)
        {
            if (session == null || !session.Name.Equals(obj.Channel, StringComparison.OrdinalIgnoreCase))
                return;

            var name = obj.ReceiverUserId;
            var player = playerProvider.GetByUserId(obj.ReceiverUserId);
            if (player != null)
            {
                name = player.DisplayName;
            }

            logger.LogDebug("[TWITCH] Sub Recieved (Channel: " + obj.Channel + " From: " + obj.DisplayName + " To: " + name + ")");
            await SendAsync("twitch_sub", obj);
        }

        private void OnUserLeft(UserLeftEvent obj) => logger.LogDebug("[TWITCH] " + " User left the channel (User: " + obj.Name + ")");
        private void OnUserJoined(UserJoinedEvent obj) => logger.LogDebug("[TWITCH] " + " User joined the channel (User: " + obj.Name + ")");

        public void Close()
        {
            this.client.Close();
        }
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
            this.messageBus.Send(MessageBus.Broadcast, new SessionGameMessageResponse(Session, obj));
        }

        private void EnqueueRequest(string request)
        {
            this.requests.Enqueue(request);
        }

        public Task<bool> ProcessAsync(int serverPort)
        {
            return Task.FromResult(true);
        }

        public override string ToString()
        {
            var str = "";
            if (this.session != null)
            {

                str += "Session Name: " + this.session.Name + " ";
            }
            str += "EndPoint: " + EndPointString;

            return str;
        }
    }
}