using RavenBot.Core;
using System.Net.Sockets;

namespace Shinobytes.Network
{
    public class ServerClientProvider : IServerClientProvider
    {
        public ServerClientProvider()
        {
        }

        public IServerClient Get(TcpClient connection)
        {
            return new ServerClient(connection);
        }
    }
}
