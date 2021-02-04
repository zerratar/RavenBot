using System.Net.Sockets;

namespace ROBot.Core.GameServer
{
    public interface IRavenfallConnectionProvider
    {
        IRavenfallConnection Get(IBotServer server, TcpClient client);
    }
}