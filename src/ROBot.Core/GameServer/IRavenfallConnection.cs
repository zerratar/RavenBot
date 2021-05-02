using RavenBot.Core.Ravenfall;
using System;
using System.Net;

namespace ROBot.Core.GameServer
{
    public interface IRavenfallConnection : IRavenfallClient, IDisposable
    {
        Guid InstanceId { get; }
        event EventHandler<GameSessionInfo> OnSessionInfoReceived;
        IGameSession Session { get; set; }
        IPEndPoint EndPoint { get; }
        string EndPointString { get; }
        void Close();
    }
}