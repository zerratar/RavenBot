using Microsoft.Extensions.Logging;
using ROBot.Core.GameServer;
using Shinobytes.Network;
using System.Text;
using System.Threading.Tasks;

namespace ROBot.LogServer.PacketHandlers
{
    public class SessionListPacketHandler : IServerPacketHandler
    {
        private readonly ILogger logger;
        private readonly IGameSessionManager sessionManager;

        public SessionListPacketHandler(ILogger logger, IGameSessionManager sessionManager)
        {
            this.logger = logger;
            this.sessionManager = sessionManager;
        }

        public Task HandleAsync(INetworkClient client, ServerPacket packet)
        {
            this.logger.LogDebug("Session List Packet Received");

            var allSessions = sessionManager.All();

            //var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(allSessions);

            var header = UTF8Encoding.UTF8.GetBytes("=+ Session List #" + allSessions.Count + System.Environment.NewLine);
            client.Send(header, 0, header.Length);


            foreach (var session in allSessions)
            {
                var data = UTF8Encoding.UTF8.GetBytes(FormatSessionRow(session) + System.Environment.NewLine);
                client.Send(data, 0, data.Length);
            }

            var footer = UTF8Encoding.UTF8.GetBytes("=- Session List" + System.Environment.NewLine);
            client.Send(footer, 0, footer.Length);
            return Task.CompletedTask;
        }

        private string FormatSessionRow(Core.IGameSession session)
        {
            var sb = new StringBuilder();
            sb.Append(session.Id);
            sb.Append("\t");
            sb.Append(session.UserId);
            sb.Append("\t");
            sb.Append(session.Name);
            sb.Append("\t");
            sb.Append(session.UserCount);
            sb.Append("\t");
            sb.Append(session.Created);
            return sb.ToString();
        }
    }
}
