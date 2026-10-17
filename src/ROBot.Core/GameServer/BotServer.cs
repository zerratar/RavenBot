using Microsoft.Extensions.Logging;
using RavenBot.Core.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ROBot.Core.GameServer
{
    public class BotServer : IBotServer
    {
        private const string BotStatusPingIP = "216.245.221.92";

        // A game that loses its link reconnects right away, so allow a fair number of tries
        // per address before refusing; 4 a minute could stall a game after a few quick drops.
        private const int MaxConnectionsPerAddressPerMinute = 20;

        // When a game link drops, its session (and the chat channel) is kept this long so a quick
        // reconnect takes it over without the bot leaving and rejoining the channel. Commands typed
        // meanwhile wait in the pending queue and are sent on the new link.
        private static readonly TimeSpan SessionHoldTime = TimeSpan.FromSeconds(90);

        private readonly ILogger logger;
        private readonly IGameSessionManager sessionManager;
        private readonly IRavenfallConnectionProvider connectionProvider;
        private readonly IBotServerSettings settings;
        private TcpListener server;
        private bool disposed;
        private ServerState state;
        private Thread connectionAcceptThread;
        private readonly List<IRavenfallConnection> connections = new List<IRavenfallConnection>();
        private readonly object connectionMutex = new object();
        private readonly object SessionAuthMutex = new object();
        private readonly PendingCommandQueue pendingCommands = new PendingCommandQueue();

        private ConcurrentDictionary<string, RemoteEndpointStatus> remoteStatus = new();

        public BotServer(
            ILogger logger,
            IGameSessionManager sessionProvider,
            IRavenfallConnectionProvider connectionProvider,
            IBotServerSettings settings)
        {
            this.connectionProvider = connectionProvider;
            this.logger = logger;
            this.sessionManager = sessionProvider;
            this.settings = settings;
            this.server = new TcpListener(IPAddress.Any, settings.ServerPort);
        }

        public IRavenfallConnection GetConnection(IGameSession ravenfallGameSession)
        {
            if (ravenfallGameSession == null)
            {
#if DEBUG
                logger.LogDebug("[RVNFLL] BotServer::GetConnection Trying to get connection using null game session.");
#endif
                return null;
            }

            lock (connectionMutex)
            {
                // a link that is up beats one held after a drop.
                return connections
                    .Where(x => x.Session?.Name == ravenfallGameSession.Name)
                    .OrderByDescending(x => x.IsConnected)
                    .FirstOrDefault();
            }
        }

        public IRavenfallConnection GetConnectionByUserId(Guid ravenfallId)
        {
            if (ravenfallId == Guid.Empty)
            {
#if DEBUG
                logger.LogDebug("[RVNFLL] BotServer::GetConnectionByUserId Trying to get connection using null or empty user id.");
#endif
                return null;
            }

            lock (connectionMutex)
            {
                return connections
                    .Where(x => x?.Session?.RavenfallUserId == ravenfallId)
                    .OrderByDescending(x => x.IsConnected)
                    .FirstOrDefault();
            }
        }

        public IReadOnlyList<IRavenfallConnection> AllConnections()
        {
            lock (connectionMutex)
            {
                return connections.ToList();
            }
        }

        public void Start()
        {

            if (this.state == ServerState.Started)
            {
                logger.LogWarning("[RVNFLL] Trying to start the server while it is already running.");
                return;
            }

            try
            {

                connectionAcceptThread = new System.Threading.Thread(AcceptIncomingConnections);
                connectionAcceptThread.Start();

            }
            catch (Exception exc)
            {
                logger.LogError(exc.ToString());
            }
        }

        private void AcceptIncomingConnections()
        {
            try
            {
                server.Start();
                state = ServerState.Started;

                while (this.state == ServerState.Started)
                {
                    // server(new AsyncCallback(OnClientConnected), null);
                    OnClientConnected(server.AcceptTcpClient());
                }
            }
            catch (Exception exc)
            {
                state = ServerState.Faulted;
                TryRestartingServer();
            }
        }

        public void OnClientDisconnected(IRavenfallConnection connection)
        {
            var session = connection.Session;
            if (session != null && state == ServerState.Started)
            {
                // keep the session for a while so a reconnect can take it over.
                connection.OnSessionInfoReceived -= Connection_OnSessionInfoReceived;
                logger.LogDebug("[RVNFLL] Ravenfall client link dropped, holding the session for " + (int)SessionHoldTime.TotalSeconds + "s. (User: " + session.Name + " EndPoint: " + connection.EndPointString + ")");
                _ = ReleaseHeldSessionAsync(connection);
            }
            else
            {
                RemoveConnection(connection);
            }

            IRavenfallConnection[] badConnections;
            lock (connectionMutex)
            {
                badConnections = connections.Where(x => x != null && x.EndPointString == "Unknown").ToArray();
            }

            var badConnectionCount = badConnections.Length;
            foreach (var badConnection in badConnections)
            {
                RemoveConnection(badConnection);
            }
            var fromStr = connection.Session?.Name ?? connection.EndPointString;
            logger.LogDebug("[RVNFLL] Ravenfall client disconnected. (EndPoint: " + fromStr + ")");
            if (badConnectionCount > 0)
            {
                logger.LogDebug("[RVNFLL] Cleaned up bad connections (Count: " + badConnectionCount + ")");
            }
        }

        private async Task ReleaseHeldSessionAsync(IRavenfallConnection connection)
        {
            try
            {
                await Task.Delay(SessionHoldTime).ConfigureAwait(false);

                lock (SessionAuthMutex)
                {
                    bool stillHeld;
                    lock (connectionMutex)
                    {
                        stillHeld = connections.Contains(connection);
                    }

                    // taken over by a new link already.
                    if (!stillHeld)
                    {
                        return;
                    }

                    var session = connection.Session;
                    RemoveConnection(connection);

                    if (session != null)
                    {
                        logger.LogDebug("[RVNFLL] Ravenfall client did not come back, session ended. (User: " + session.Name + ")");
                        if (session.RavenfallUserId != Guid.Empty && GetConnectionByUserId(session.RavenfallUserId) == null)
                        {
                            pendingCommands.Clear(session.RavenfallUserId);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                logger.LogError("[RVNFLL] Failed to release a held session: " + exc);
            }
        }

        public bool EnqueuePendingRequest(Guid ravenfallUserId, string request)
        {
            if (ravenfallUserId == Guid.Empty || string.IsNullOrEmpty(request))
            {
                return false;
            }

            // another link may already serve this session, send it there right away.
            IRavenfallConnection live;
            lock (connectionMutex)
            {
                live = connections.FirstOrDefault(x => x.IsConnected && x.Session?.RavenfallUserId == ravenfallUserId);
            }

            if (live != null)
            {
                _ = SendRawAsync(live, request);
                return true;
            }

            pendingCommands.Enqueue(ravenfallUserId, request);
            return true;
        }

        private async Task SendRawAsync(IRavenfallConnection connection, string request)
        {
            try
            {
                await connection.SendRawAsync(request).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                logger.LogError("[RVNFLL] Failed to send a queued command: " + exc);
            }
        }

        private async Task SendPendingCommandsAsync(IRavenfallConnection connection, Guid ravenfallUserId)
        {
            try
            {
                var requests = pendingCommands.TakeAll(ravenfallUserId);
                if (requests.Count == 0)
                {
                    return;
                }

                logger.LogDebug("[RVNFLL] Sending " + requests.Count + " command(s) queued while the game was away. (User: " + connection.Session?.Name + ")");
                for (var i = 0; i < requests.Count; ++i)
                {
                    if (!connection.IsConnected)
                    {
                        // dropped again, keep the rest for the next link.
                        for (var j = i; j < requests.Count; ++j)
                        {
                            pendingCommands.Enqueue(ravenfallUserId, requests[j]);
                        }
                        return;
                    }

                    await connection.SendRawAsync(requests[i]).ConfigureAwait(false);
                }
            }
            catch (Exception exc)
            {
                logger.LogError("[RVNFLL] Failed to send queued commands: " + exc);
            }
        }

        private void RemoveConnection(IRavenfallConnection connection)
        {
            connection.OnSessionInfoReceived -= Connection_OnSessionInfoReceived;

            var session = connection.Session;
            var sessionStillInUse = false;
            lock (connectionMutex)
            {
                connections.Remove(connection);
                if (session != null)
                {
                    sessionStillInUse = connections.Any(x => x.Session == session);
                }
            }

            // the session manager raises events, so never call it while holding the connection lock.
            if (session != null && !sessionStillInUse)
            {
                sessionManager.Remove(session);
            }
        }

        private bool TryRestartingServer()
        {
            try
            {
                if (server == null || server.Server == null || !server.Server.IsBound)
                {
                    try
                    {
                        server.Server.Dispose();
                        server = null;
                    }
                    catch { }

                    IRavenfallConnection[] toDispose;
                    lock (connectionMutex)
                    {
                        toDispose = connections.ToArray();
                        connections.Clear();
                    }

                    foreach (var c in toDispose)
                    {
                        this.sessionManager.Remove(c.Session);

                        try
                        {
                            c.Dispose();
                        }
                        catch { }
                    }

                    this.sessionManager.ClearAll();

                    this.server = new TcpListener(IPAddress.Any, settings.ServerPort);
                    this.Start();
                    return true;
                }
            }
            catch (Exception exc2)
            {
                logger.LogError("[RVNFLL] Unable to recover from bad server state (Exception: " + exc2 + ")");
            }
            return false;
        }

        private void OnClientConnected(TcpClient client)
        {
            try
            {
                //var client = server.EndAcceptTcpClient(ar);
                if (client != null)
                {
                    if (HandlePingRequest(client))
                        return;

                    if (client.Client.RemoteEndPoint is IPEndPoint endpoint)
                    {
                        var address = endpoint.Address;

                        // check if address is blacklisted or temporarily suspended (for instance: trying to reconnect too frequently)
                        if (!CheckIfAddressIsAcceptable(address))
                        {
                            client.Dispose();
                            return;
                        }
                    }

                    var connection = connectionProvider.Get(this, client);
                    if (connection == null)
                    {
#if DEBUG
                        logger.LogDebug("[RVNFLL] BotServer::OnClientConnected Trying to get connection returned null.");
#endif
                        return;
                    }


                    logger.LogDebug("[RVNFLL] Ravenfall client connected. (EndPoint: " + connection.EndPointString + ")");

                    lock (connectionMutex)
                    {
                        connections.Add(connection);
                    }

                    connection.OnSessionInfoReceived += Connection_OnSessionInfoReceived;
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc.ToString());
            }
        }
        private bool CheckIfAddressIsAcceptable(IPAddress address)
        {
            var addr = address.ToString();
            if (!remoteStatus.TryGetValue(addr, out var status))
            {
                status = new RemoteEndpointStatus();
            }

            var ss = status.SampleStart;
            var now = DateTime.UtcNow;

            status.LastConnectionUtc = now;

            if (now - ss > TimeSpan.FromMinutes(1))
            {
                status.SampleStart = now;
                status.ConnectionTriesThisMinute = 1;
            }
            else
            {
                status.ConnectionTriesThisMinute++;
            }
            status.TotalCconnectionTries++;

            remoteStatus[addr] = status;

            if (status.ConnectionTriesThisMinute > MaxConnectionsPerAddressPerMinute)
            {
                logger.LogDebug("[RVNFLL] Refused a connection, too many tries this minute. (Address: " + addr + " Tries: " + status.ConnectionTriesThisMinute + ")");
                return false;
            }

            return true;
        }

        private bool HandlePingRequest(TcpClient client)
        {
            if (client.Client.RemoteEndPoint != null)
            {
                try
                {
                    var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    var addr = endpoint.Address?.ToString();
                    if (addr != null)
                    {
                        if (addr.Equals(BotStatusPingIP, StringComparison.OrdinalIgnoreCase))
                        {
#if DEBUG
                            logger.LogDebug("[RVNFLL] Bot Status Ping Recieved.");
#endif
                            client.Close();
                            return true;
                        }
                    }
                }
                catch
                {
                    // Ignored.
                }
            }
            return false;
        }

        private void Connection_OnSessionInfoReceived(object sender, RemoteGameSessionInfo e)
        {
            if (!(sender is IRavenfallConnection connection))
            {
                return;
            }

            lock (SessionAuthMutex)
            {
                // the same link sending its session info again only refreshes the owner.
                var current = connection.Session;
                if (current != null && current.RavenfallUserId == e.UserId)
                {
                    sessionManager.Update(current.Id, e.UserId, e.Owner);
                    return;
                }

                // other links (live or held after a drop) for the same user
                IRavenfallConnection[] others;
                lock (connectionMutex)
                {
                    others = connections
                        .Where(x => x != null && x.InstanceId != connection.InstanceId && x.Session != null && x.Session.RavenfallUserId == e.UserId)
                        .ToArray();
                }

                // a live link with a newer session wins; a dropped link or the same session never does.
                foreach (var other in others)
                {
                    var otherSession = other.Session;
                    if (otherSession != null && otherSession.Id != e.SessionId && other.IsConnected
                        && SessionCreatedTime.IsOlder(e.Created, otherSession.Created))
                    {
                        sessionManager.Update(otherSession.Id, e.UserId, e.Owner);
                        logger.LogDebug("[RVNFLL] Ravenfall client sent a second auth with a created date less than current. (EndPoint: " + connection.EndPointString + ")");
                        return;
                    }
                }

                // take over: a link for the same session keeps the session (and the chat channel),
                // a different session ends the old one as before.
                IGameSession reused = null;
                var ended = new List<IGameSession>();
                foreach (var other in others)
                {
                    var otherSession = other.Session;
                    if (otherSession == null)
                    {
                        continue;
                    }

                    // detach first so closing the old link neither ends the session nor holds it.
                    other.Session = null;
                    if (otherSession.Id == e.SessionId && (reused == null || reused == otherSession))
                    {
                        reused = otherSession;
                    }
                    else if (!ended.Contains(otherSession))
                    {
                        ended.Add(otherSession);
                    }

                    other.Close();
                    RemoveConnection(other);
                }

                foreach (var s in ended)
                {
                    if (s != reused)
                    {
                        sessionManager.Remove(s);
                    }
                }

                if (reused != null)
                {
                    connection.Session = reused;
                    sessionManager.Update(reused.Id, e.UserId, e.Owner);
                    logger.LogDebug("[RVNFLL] Ravenfall client reconnected and took over its session. (TwitchName: " + e.Owner.Username + " User: " + reused.Name + " EndPoint: " + connection.EndPointString + ")");
                }
                else
                {
                    connection.Session = sessionManager.Add(this, e.SessionId, e.UserId, e.Owner, e.Created);
                    logger.LogDebug("[RVNFLL] Ravenfall client authenticated. (TwitchName: " + e.Owner.Username + " User: " + connection.Session.Name + " EndPoint: " + connection.EndPointString + ")");
                }
            }

            _ = SendPendingCommandsAsync(connection, e.UserId);
        }

        public void Dispose()
        {
            if (disposed) return;
            if (server.Server.IsBound)
                server.Stop();
            disposed = true;
            this.state = ServerState.Disposed;
        }

        public IGameSession GetSession(ICommandChannel channel)
        {
            var s = sessionManager.GetByChannelId(channel.Id);
            if (s == null)
            {
                s = sessionManager.GetByName(channel.Name);
            }
            if (s != null)
            {
                s.Channel = channel;
                return s;
            }

            return null;
        }
    }

    public struct RemoteEndpointStatus
    {
        public DateTime LastConnectionUtc;
        public int TotalCconnectionTries;
        public int ConnectionTriesThisMinute;
        public DateTime SampleStart;
    }

    public enum ServerState
    {
        NotStarted,
        Started,
        Faulted,
        Disposed
    }
}
