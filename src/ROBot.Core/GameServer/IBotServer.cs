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

        /// <summary>
        /// Holds a command for the user's game session while its link is down, or sends it right away
        /// if another link already serves that session. Returns false if it could not be taken.
        /// </summary>
        bool EnqueuePendingRequest(Guid ravenfallUserId, string request);
    }
}
