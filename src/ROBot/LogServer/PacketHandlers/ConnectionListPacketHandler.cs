using Microsoft.Extensions.Logging;
using ROBot.Core.GameServer;
using Shinobytes.Network;
using System.Text;
using System.Threading.Tasks;

namespace ROBot.LogServer.PacketHandlers
{
    public class ConnectionListPacketHandler : IServerPacketHandler
    {
        private readonly ILogger logger;
        private readonly IBotServer server;

        public ConnectionListPacketHandler(ILogger logger, IBotServer server)
        {
            this.logger = logger;
            this.server = server;
        }

        public Task HandleAsync(INetworkClient client, ServerPacket packet)
        {
            this.logger.LogDebug("[LOG] Connection List Packet Received");

            var allConnections = server.AllConnections();
            var header = UTF8Encoding.UTF8.GetBytes("=+ Connection List #" + allConnections.Count + System.Environment.NewLine);
            client.Send(header, 0, header.Length);

            var data = UTF8Encoding.UTF8.GetBytes(ReturnConnectionHeader() + System.Environment.NewLine);
            client.Send(data, 0, data.Length);

            foreach (var connection in allConnections)
            {
                var data = UTF8Encoding.UTF8.GetBytes(FormatConnectionRow(connection) + System.Environment.NewLine);
                client.Send(data, 0, data.Length);
            }

            var footer = UTF8Encoding.UTF8.GetBytes("=- Connection List" + System.Environment.NewLine);
            client.Send(footer, 0, footer.Length);
            return Task.CompletedTask;
        }

        private string FormatConnectionRow(IRavenfallConnection connection)
        {
            var sb = new StringBuilder();
            sb.Append(connection.InstanceId);
            sb.Append("\t");
            sb.Append(connection.EndPointString);
            if (connection.Session != null)
            {
                sb.Append("\t");
                sb.Append(connection.Session.UserId);
                sb.Append("\t");
                sb.Append(connection.Session.Name);
                sb.Append("\t");
                sb.Append(connection.Session.Created);
            }
            return sb.ToString();
        }
        private string ReturnConnectionHeader()
        {
            var sb = new StringBuilder();
            sb.Append("InstanceId");
            sb.Append("\t");
            sb.Append("EndPointString");
            
            sb.Append("\t");
            sb.Append("Session.UserId");
            sb.Append("\t");
            sb.Append("Session.Name");
            sb.Append("\t");
            sb.Append("Session.Created");
            
            return sb.ToString();
        }
    }
}
