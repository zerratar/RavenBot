using Microsoft.Extensions.Logging;
using ROBot.Core.Twitch;
using Shinobytes.Network;
using Shinobytes.Ravenfall.RavenNet.Core;
using System.IO;
using System.Text.Unicode;
using System.Threading.Tasks;
using TwitchLib.Api.ThirdParty.UsernameChange;
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
            if (packet.Data == null || (packet.Data.Buffer?.Length ?? 0) == 0)
            {
                this.logger.LogError("An empty PubSub Packet Received");
                return Task.CompletedTask;
            }

            this.logger.LogDebug("PubSub Packet Received");

            string userId = null;
            string userName = null;
            string token = null;
            try
            {
                using (var mem = new MemoryStream(packet.Data.Buffer, packet.Data.Offset, packet.Data.Length))
                using (var reader = new BinaryReader(mem))
                {
                    short size = 0;

                    size = reader.ReadInt16();
                    userId = System.Text.UTF8Encoding.UTF8.GetString(reader.ReadBytes(size));

                    size = reader.ReadInt16();
                    userName = System.Text.UTF8Encoding.UTF8.GetString(reader.ReadBytes(size));

                    size = reader.ReadInt16();
                    token = System.Text.UTF8Encoding.UTF8.GetString(reader.ReadBytes(size));

                    var data = pubSubRepo.AddOrUpdate(userId, userName, token);
                    messageBus.Send("pubsub", data);

                    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(token))
                    {
                        this.logger.LogError("Bad pubsub data received: "
                            + ", userId: " + userId
                            + ", userName: " + userName
                            + ", token: " + token
                        );
                    }
                }
            }
            catch (System.Exception exc)
            {
                this.logger.LogError("Bad pubsub data received: " + exc
                    + ", userId: " + userId
                    + ", userName: " + userName
                    + ", token: " + token
                    );
            }
            return Task.CompletedTask;
        }
    }
}
