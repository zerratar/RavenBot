using RavenBot.Core;
using System;
using System.Net.Sockets;

namespace Shinobytes.Network
{
    public class ServerClient : NetworkClient, IServerClient
    {
        public ServerClient(TcpClient connection) : base(connection)
        {
        }
    }
}
