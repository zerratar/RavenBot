using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ROBot.Core.GameServer
{
    public class BotServer : IBotServer
    {
        private readonly ILogger logger;
        private readonly IGameSessionManager sessionManager;
        private readonly IRavenfallConnectionProvider connectionProvider;
        private readonly IBotServerSettings settings;
        private readonly TcpListener server;
        private bool disposed;

        private readonly List<IRavenfallConnection> connections = new List<IRavenfallConnection>();
        private readonly object connectionMutex = new object();
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
            lock (connectionMutex)
            {
                return connections.FirstOrDefault(x => x.Session?.Name == ravenfallGameSession.Name);
            }
        }

        public IRavenfallConnection GetConnectionByUserId(string sessionUserId)
        {
            lock (connectionMutex)
            {
                return connections.FirstOrDefault(x => x.Session?.UserId == sessionUserId);
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
            try
            {
                if (server.Server.IsBound)
                {
                    return;
                }

                server.Start();
                server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), null);
            }
            catch (Exception exc)
            {
                logger.LogError(exc.ToString());
            }
        }

        public void OnClientDisconnected(IRavenfallConnection connection)
        {
            lock (connectionMutex)
            {
                connection.OnSessionInfoReceived -= Connection_OnSessionInfoReceived;
                if (connection.Session != null)
                {
                    sessionManager.Remove(connection.Session);
                }
                connections.Remove(connection);

                foreach (var badConnection in connections.Where(x => x.EndPointString == "Unknown").ToArray())
                {
                    try { badConnection.Dispose(); } catch { }
                    connections.Remove(badConnection);
                }

                logger.LogDebug("[" + connection.EndPointString + "] Ravenfall client disconnected.");
            }
        }

        private void OnClientConnected(IAsyncResult ar)
        {
            try
            {
                var client = server.EndAcceptTcpClient(ar);
                if (client != null)
                {
                    var connection = connectionProvider.Get(this, client);
                    logger.LogDebug("[" + connection.EndPointString + "] Ravenfall client connected.");
                    connections.Add(connection);
                    connection.OnSessionInfoReceived += Connection_OnSessionInfoReceived;
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc.ToString());
            }
            try
            {
                server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), null);
            }
            catch { }
        }

        private void Connection_OnSessionInfoReceived(object sender, GameSessionInfo e)
        {
            lock (SessionAuthMutex)
            {
                if (sender is IRavenfallConnection connection)
                {
                    // check for existing connections using the same session details
                    var existingConnection = GetConnectionByUserId(e.TwitchUserId);
                    if (existingConnection != null)
                    {
                        if (existingConnection.InstanceId != connection.InstanceId)
                        {
                            if (existingConnection.Session.Created > e.Created)
                            {
                                logger.LogDebug("[" + connection.EndPointString + "] Ravenfall client sent a second auth with a created date less than current.");
                                return;
                            }

                            existingConnection.Close();
                        }
                        else if (existingConnection.Session != null)
                        {
                            return;
                        }
                    }

                    connection.Session = sessionManager.Add(this, e.SessionId, e.TwitchUserId, e.TwitchUserName, e.Created);
                    logger.LogDebug("[" + connection.EndPointString + "] Ravenfall client authenticated. User: " + connection.Session.Name);
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            if (server.Server.IsBound)
                server.Stop();
            disposed = true;
        }

        public IGameSession GetSession(string session)
        {
            var s = sessionManager.GetByName(session);
            if (s == null)
                return sessionManager.GetByUserId(session);
            return s;
        }
    }
}
