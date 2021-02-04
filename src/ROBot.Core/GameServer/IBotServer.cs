using Shinobytes.Ravenfall.RavenNet;
using System;

namespace ROBot.Core.GameServer
{
    public interface IBotServer : IDisposable
    {
        void Start();
        IGameSession GetSession(string session);
        void OnClientDisconnected(IRavenfallConnection connection);
        IRavenfallConnection GetConnection(IGameSession ravenfallGameSession);
    }
}
