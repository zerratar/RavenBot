using Shinobytes.Ravenfall.RavenNet;
using System;
using System.Collections.Generic;

namespace ROBot.Core.GameServer
{
    public interface IBotServer : IDisposable
    {
        void Start();
        IReadOnlyList<IRavenfallConnection> AllConnections();
        IGameSession GetSession(string session);
        void OnClientDisconnected(IRavenfallConnection connection);
        IRavenfallConnection GetConnection(IGameSession ravenfallGameSession);
    }
}
