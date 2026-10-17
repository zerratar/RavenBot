using RavenBot.Core.Handlers;
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

        event EventHandler<RemoteGameSessionInfo> OnSessionInfoReceived;
        IGameSession Session { get; set; }
        IPEndPoint EndPoint { get; }
        string EndPointString { get; }
        IRavenfallApi Api { get; }

        /// <summary>True while the game link is up.</summary>
        bool IsConnected { get; }

        /// <summary>Sends an already serialized game message as is, used to send commands queued while the link was down.</summary>
        Task SendRawAsync(string request);
        IRavenfallApi Ref(string correlationId);
        IRavenfallApi this[string correlationid] { get; }
        IRavenfallApi this[ICommand cmd] { get; }
        void Close();
    }
}