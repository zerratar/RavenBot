using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shinobytes.Network
{
    public interface IServer : IDisposable
    {
        event EventHandler<ConnectionEventArgs> ClientConnected;
        event EventHandler<ConnectionEventArgs> ClientDisconnected;
        Task StartAsync(CancellationToken cancellationToken);
        void Stop();
    }
}
