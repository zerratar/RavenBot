using Microsoft.Extensions.Logging;
using ROBot.Core.Twitch;
using Shinobytes.Network;
using Shinobytes.Ravenfall.RavenNet.Core;
using System.IO;
using System.Text.Unicode;
using System.Threading.Tasks;
using TwitchLib.PubSub.Interfaces;

namespace ROBot.LogServer.PacketHandlers
{
    public class PubSubPacketHandler : IServerPacketHandler
    {
        private readonly ILogger logger;
        private readonly IMessageBus messageBus;
        private readonly ITwitchPubSubTokenRepository pubSubRepo;

        public PubSubPacketHandler(
            ILogger logger,
            IMessageBus messageBus,
            ITwitchPubSubTokenRepository pubSubRepo)
        {
            this.logger = logger;
            this.messageBus = messageBus;
            this.pubSubRepo = pubSubRepo;
        }

        public Task HandleAsync(INetworkClient client, ServerPacket packet)
        {
            this.logger.LogDebug("PubSub Packet Received");

            if (packet.Data == null || (packet.Data.Buffer?.Length ?? 0) == 0)
            {
                return Task.CompletedTask;
            }
            try
            {
                using (var mem = new MemoryStream(packet.Data.Buffer, packet.Data.Offset, packet.Data.Length))
                using (var reader = new BinaryReader(mem))
                {
                    short size = 0;
                    
                    size = reader.ReadInt16();
                    var userId = System.Text.UTF8Encoding.UTF8.GetString(reader.ReadBytes(size));

                    size = reader.ReadInt16();
                    var userName = System.Text.UTF8Encoding.UTF8.GetString(reader.ReadBytes(size));

                    size = reader.ReadInt16();
                    var token = System.Text.UTF8Encoding.UTF8.GetString(reader.ReadBytes(size));
                    
                    var data = pubSubRepo.AddOrUpdate(userId, userName, token);
                    messageBus.Send("pubsub", data);
                }
            }
            catch { }
            return Task.CompletedTask;
        }
    }
}
