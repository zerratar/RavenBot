using RavenBot.Core.Handlers;
using System;
using System.Collections.Generic;

namespace ROBot.Core.GameServer
{
    public interface IBotServer : IDisposable
    {
        void Start();
        IReadOnlyList<IRavenfallConnection> AllConnections();
        IGameSession GetSession(ICommandChannel session);
        void OnClientDisconnected(IRavenfallConnection connection);
        IRavenfallConnection GetConnection(IGameSession ravenfallGameSession);
        IRavenfallConnection GetConnectionByUserId(Guid ravenfallUserId);
    }
}
