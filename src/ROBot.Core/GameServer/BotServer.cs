using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ROBot.Core.GameServer
{
    public class BotServer : IBotServer
    {
        private const string BotStatusPingIP = "216.245.221.92";

        private readonly ILogger logger;
        private readonly IGameSessionManager sessionManager;
        private readonly IRavenfallConnectionProvider connectionProvider;
        private readonly IBotServerSettings settings;
        private TcpListener server;
        private bool disposed;
        private ServerState state;
        private Thread connectionAcceptThread;
        private readonly List<IRavenfallConnection> connections = new List<IRavenfallConnection>();
        //private readonly object connectionMutex = new object();
        private readonly object SessionAuthMutex = new object();

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
            //lock (connectionMutex)
            {
                if (ravenfallGameSession == null)
                {
#if DEBUG
                    logger.LogDebug("[RVNFLL] BotServer::GetConnection Trying to get connection using null game session.");
#endif
                    return null;
                }

                return connections.FirstOrDefault(x => x.Session?.Name == ravenfallGameSession.Name);
            }
        }

        public IRavenfallConnection GetConnectionByUserId(Guid ravenfallId)
        {
            //lock (connectionMutex)
            {
                if (ravenfallId == Guid.Empty)
                {
#if DEBUG
                    logger.LogDebug("[RVNFLL] BotServer::GetConnectionByUserId Trying to get connection using null or empty user id.");
#endif
                    return null;
                }

                return connections.FirstOrDefault(x => x?.Session?.RavenfallUserId == ravenfallId);
            }
        }

        public IReadOnlyList<IRavenfallConnection> AllConnections()
        {
            //lock (connectionMutex)
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
            RemoveConnection(connection);

            var badConnectionCount = 0;
            //lock (connectionMutex)
            {
                var badConnections = connections.Where(x => x != null && x.EndPointString == "Unknown").ToArray();
                badConnectionCount = badConnections.Length;
                foreach (var badConnection in badConnections)
                {
                    RemoveConnection(badConnection);
                }
            }
            var fromStr = connection.Session?.Name ?? connection.EndPointString;
            logger.LogDebug("[RVNFLL] Ravenfall client disconnected. (EndPoint: " + fromStr + ")");
            if (badConnectionCount > 0)
            {
                logger.LogDebug("[RVNFLL] Cleaned up bad connections (Count: " + badConnectionCount + ")");
            }
        }

        private void RemoveConnection(IRavenfallConnection connection)
        {
            //lock (connectionMutex)
            {
                connection.OnSessionInfoReceived -= Connection_OnSessionInfoReceived;

                if (connection.Session != null)
                {
                    sessionManager.Remove(connection.Session);
                }

                connections.Remove(connection);
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

                    {
                        foreach (var c in this.connections)
                        {
                            this.sessionManager.Remove(c.Session);

                            try
                            {
                                c.Dispose();
                            }
                            catch { }
                        }
                        connections.Clear();
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

                    var connection = connectionProvider.Get(this, client);
                    if (connection == null)
                    {
#if DEBUG
                        logger.LogDebug("[RVNFLL] BotServer::OnClientConnected Trying to get connection returned null.");
#endif
                        return;
                    }

                    logger.LogDebug("[RVNFLL] Ravenfall client connected. (EndPoint: " + connection.EndPointString + ")");

                    //lock (connectionMutex)
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

        private void Connection_OnSessionInfoReceived(object sender, GameSessionInfo e)
        {
            lock (SessionAuthMutex)
            {
                if (sender is IRavenfallConnection connection)
                {
                    // check for existing connections using the same session details
                    var existingConnection = GetConnectionByUserId(e.UserId);
                    if (existingConnection != null)
                    {
                        if (existingConnection.Session != null)
                        {
                            sessionManager.Update(existingConnection.Session.Id, e.UserId, e.Owner);
                        }

                        if (existingConnection.InstanceId != connection.InstanceId)
                        {
                            if (existingConnection.Session.Created > e.Created)
                            {
                                logger.LogDebug("[RVNFLL] Ravenfall client sent a second auth with a created date less than current. (EndPoint: " + connection.EndPointString + ")");
                                return;
                            }

                            existingConnection.Close();
                        }
                        else if (existingConnection.Session != null)
                        {
                            return;
                        }
                    }

                    connection.Session = sessionManager.Add(this, e.SessionId, e.UserId, e.Owner, e.Created);
                    logger.LogDebug("[RVNFLL] Ravenfall client authenticated. (TwitchName: " + e.Owner.Username + " User: " + connection.Session.Name + " EndPoint: " + connection.EndPointString + ")");
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            if (server.Server.IsBound)
                server.Stop();
            disposed = true;
            this.state = ServerState.Disposed;
        }

        public IGameSession GetSession(string session)
        {
            return sessionManager.GetByName(session);
        }
    }

    public enum ServerState
    {
        NotStarted,
        Started,
        Faulted,
        Disposed
    }
}
