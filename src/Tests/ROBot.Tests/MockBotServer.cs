using ROBot.Core;
using ROBot.Core.GameServer;
using System;
using System.Collections.Generic;

namespace ROBot.Tests
{
    public class MockBotServer : IBotServer
    {
        public IReadOnlyList<IRavenfallConnection> AllConnections()
        {
            return new[] { new MockRavenfallConnection() };
        }

        public void Dispose()
        {
        }

        public IRavenfallConnection GetConnection(IGameSession ravenfallGameSession)
        {
            return new MockRavenfallConnection();
        }

        public IRavenfallConnection GetConnectionByUserId(string sessionUserId)
        {
            return new MockRavenfallConnection();
        }

        public IRavenfallConnection GetConnectionByUserId(Guid ravenfallUserId)
        {
            throw new NotImplementedException();
        }

        public IGameSession GetSession(string session)
        {
            return new MockGameSession(session);
        }

        public void OnClientDisconnected(IRavenfallConnection connection)
        {
        }

        public void Start()
        {
        }
    }

}