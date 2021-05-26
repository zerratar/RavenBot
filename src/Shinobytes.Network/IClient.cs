using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Shinobytes.Network
{
    public interface IClient : INetworkClient
    {
        bool Connected { get; }

        Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken);
    }
}
