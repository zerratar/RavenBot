using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shinobytes.Network
{
    public interface IServer : IDisposable
    {
        event EventHandler<ConnectionEventArgs> ClientConnected;
        event EventHandler<ConnectionEventArgs> ClientDisconnected;
        Task<bool> StartAsync(CancellationToken cancellationToken);
        Exception LastException { get; }
        void Stop();
    }
}
