using Microsoft.Extensions.Logging;
using ROBot.Core.Twitch;
using Shinobytes.Network;
using Shinobytes.Ravenfall.RavenNet.Core;
using System.Threading.Tasks;

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
                this.logger.LogError("An empty PubSub Packet Recieved");
                return Task.CompletedTask;
            }

            string userId = null;
            string userName = null;
            string token = null;
            try
            {
                using (var reader = packet.Data.GetReader())
                {
                    userId = reader.ReadString();
                    userName = reader.ReadString();
                    token = reader.ReadString();                    

                    messageBus.Send("pubsub", pubSubRepo.AddOrUpdate(userId, userName, token));
                    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(token))
                    {
                        this.logger.LogError("Bad pubsub data recieved: "
                            + ", userId: " + userId
                            + ", userName: " + userName
                            + ", token: " + token
                        );
                    }
                }
            }
            catch (System.Exception exc)
            {
                this.logger.LogError("Bad pubsub data recieved: " + exc
                    + ", userId: " + userId
                    + ", userName: " + userName
                    + ", token: " + token
                    );
            }
            return Task.CompletedTask;
        }
    }
}
