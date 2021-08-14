using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Models;
using System;
using System.Net;
using System.Threading.Tasks;

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