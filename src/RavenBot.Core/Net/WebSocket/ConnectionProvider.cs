using System.Collections.Generic;
using System.Linq;

namespace RavenBot.Core.Net.WebSocket
{
    public class ConnectionProvider : IConnectionProvider
    {
        private readonly ILogger logger;
        private readonly IPacketDataSerializer packetDataSerializer;
        private readonly List<IConnection> connections = new List<IConnection>();
        private readonly object mutex = new object();
        public ConnectionProvider(ILogger logger, IPacketDataSerializer packetDataSerializer)
        {
            this.logger = logger;
            this.packetDataSerializer = packetDataSerializer;
        }

        public IReadOnlyList<IConnection> GetAll()
        {
            lock (mutex)
            {
                return connections.ToList();
            }
        }

        public IConnection Get(System.Net.WebSockets.WebSocket socket)
        {
            if (socket == null)
            {
                return null;
            }
            lock (mutex)
            {
                var connection = new Connection(logger, socket, packetDataSerializer);
                connections.Add(connection);
                return connection;
            }
        }
    }
}