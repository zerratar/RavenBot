using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenBot.Core;
using RavenBot.Core.Chat;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Models;
using Shinobytes.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ROBot.Core.GameServer
{
    public class RavenfallConnection : IRavenfallConnection
    {
        private const int PingIntervalMs = 3000;

        // Old games answer pings through a slow one-command-per-frame queue, so be generous and
        // count anything received as a sign of life, not only pongs.
        private static readonly TimeSpan SilenceTimeout = TimeSpan.FromSeconds(45);

        private readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IBotServer server;
        private readonly IUserProvider playerProvider;
        private readonly IMessageBus messageBus;
        private readonly IUserSettingsManager settingsManager;
        private readonly RavenfallGameClientConnection client;
        private readonly object sessionInfoMutex = new object();
        private RemoteGameSessionInfo queuedSessionInfo;
        private IGameSession session;
        // the user this link last served, so commands sent after its session moved on still find a home.
        private Guid lastRavenfallUserId;
        private ITimeoutHandle activePing;

        private IPEndPoint endPoint;

        private int pingSendIndex = 0;
        private int pongReceiveIndex = 0;
        private int missedPingCount = 0;
        private volatile bool hasReceivedPong;
        private bool disposed;

        private RemoteGameSessionInfo sessionInfo;
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
            this.subs.Add(messageBus.Subscribe<ChannelStateChangedEvent>(nameof(ChannelStateChangedEvent), OnChannelStateChanged));
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

            // read only now that every subscription is in place.
            this.client.StartReading();
        }

        private void OnChannelStateChanged(ChannelStateChangedEvent evt)
        {
            // only interesting if its our current channel.

            // the bot's channel states are kept for the whole bot (ChannelStates), so a game that connects later is told too

            var channel = this.session?.Channel?.Name ?? sessionInfo?.Owner?.Username;
            if (this.session == null || this.session.Channel == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(evt.ChannelName) || string.IsNullOrEmpty(channel) ||
                !evt.ChannelName.Equals(channel, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var userSettings = sessionInfo?.Settings;
            if (userSettings == null
                || !userSettings.TryGetValue("client_version", out var clientVersionString)
                || GameVersion.IsLessThanOrEquals(clientVersionString?.ToString(), "0.9.1.7a"))
            {
                return;
            }

            this.Api.SendChannelStateAsync(evt.Platform, evt.ChannelName, evt.InChannel, evt.Message);
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
                // the game sends its session info once, right after connecting; if it came before
                // anyone listened, hand it over now instead of losing it.
                RemoteGameSessionInfo queued;
                lock (sessionInfoMutex)
                {
                    internalSessionInfoReceived += value;
                    queued = queuedSessionInfo;
                    queuedSessionInfo = null;
                }

                if (queued != null)
                {
                    PublishSessionInfo(queued);
                }
            }
            remove
            {
                lock (sessionInfoMutex)
                {
                    internalSessionInfoReceived -= value;
                }
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
                if (value != null && value.RavenfallUserId != Guid.Empty)
                {
                    lastRavenfallUserId = value.RavenfallUserId;
                }
            }
        }

        public bool IsConnected => this.client.IsConnected;

        public Task SendRawAsync(string request)
        {
            return this.client.SendAsync(request);
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
            hasReceivedPong = true;
        }


        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, bool> unreadableCreatedTimeLogged = new();

        private void RegisterSession(GameMessageResponse obj)
        {
            try
            {
                // TODO: should not need to make it into string here, they will have the correct
                //       formats already, so its better to just do a normal cast.

                Guid.TryParse(obj.Args[0].ToString(), out var sessionid);
                Guid.TryParse(obj.Args[1].ToString(), out var userId);
                if (!SessionCreatedTime.TryParse(obj.Args[2], out var sessionStart) && unreadableCreatedTimeLogged.TryAdd(sessionid, true))
                {
                    // the game sends its session on every connect, so once per session is enough
                    logger.LogDebug("[RVNFLL] Could not read the session created time '" + obj.Args[2] + "', treating it as unknown.");
                }

                var token = obj.Args[3] as JToken;
                var userSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(token.ToString());

                //var userSettings = obj.Args[3];

                settingsManager.Set(userId, userSettings);

                var player = playerProvider.Get(userId);
                player.IsBroadcaster = true;

                var info = new RemoteGameSessionInfo
                {
                    Created = sessionStart,
                    SessionId = sessionid,
                    UserId = userId,
                    Owner = player,
                    Settings = userSettings
                };

                this.sessionInfo = info;

                lock (sessionInfoMutex)
                {
                    if (internalSessionInfoReceived == null)
                    {
                        queuedSessionInfo = info;
                        return;
                    }
                }

                PublishSessionInfo(info);
            }
            catch (Exception exc)
            {
                logger.LogError($"RegisterSessionOwner session: {Session?.Name}, failed: " + exc);
            }
        }

        private void PublishSessionInfo(RemoteGameSessionInfo info)
        {
            try
            {
                EventHandler<RemoteGameSessionInfo> handler;
                lock (sessionInfoMutex)
                {
                    handler = internalSessionInfoReceived;
                }

                if (handler == null)
                {
                    return;
                }

                handler.Invoke(this, info);

                messageBus.Send("ravenfall_session", info);

                var userSettings = info.Settings;
                if (userSettings == null ||
                    !userSettings.TryGetValue("client_version", out var clientVersionString) ||
                    GameVersion.IsLessThanOrEquals(clientVersionString?.ToString(), "0.9.1.7a"))
                {
                    return;
                }

                if (ChannelStates.TryGet(info.Owner.Username, out var evt))
                {
                    this.Api.SendChannelStateAsync(evt.Platform, evt.ChannelName, evt.InChannel, evt.Message);
                }
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
            if (disposed)
            {
                return;
            }

            if (pingSendIndex != pongReceiveIndex)
            {
                missedPingCount++;
            }

            // Only games that have answered a ping on this link are held to the heartbeat,
            // so a game too old to answer pings is never dropped for it.
            var silentFor = DateTime.UtcNow - client.LastMessageReceivedUtc;
            if (hasReceivedPong && silentFor > SilenceTimeout)
            {
                logger.LogWarning("[RVNFLL] Nothing heard from the game for " + (int)silentFor.TotalSeconds + "s, closing the link so it can reconnect. (" + this + " Ping " + pingSendIndex + ", Pong " + pongReceiveIndex + ")");
                if (activePing != null)
                {
                    kernel.ClearTimeout(activePing);
                    activePing = null;
                }

                Close();
                return;
            }

            if (activePing != null)
                kernel.ClearTimeout(activePing);

            Api.Ping(pingSendIndex++);

            activePing = kernel.SetTimeout(() => PingPong(), PingIntervalMs);
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
            // Commands belong to the game session, not to this link: a new link for the same
            // session sends them when it takes over. Before any session is known keep them here.
            var userId = session?.RavenfallUserId ?? Guid.Empty;
            if (userId == Guid.Empty)
            {
                userId = lastRavenfallUserId;
            }

            if (userId != Guid.Empty && server.EnqueuePendingRequest(userId, request))
            {
                return;
            }

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