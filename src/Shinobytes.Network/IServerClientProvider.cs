using System.Net.Sockets;

namespace Shinobytes.Network
{
    public interface IServerClientProvider
    {
        IServerClient Get(TcpClient connection);
    }
}
