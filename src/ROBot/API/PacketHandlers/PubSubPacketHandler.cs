using Microsoft.Extensions.Logging;
using ROBot.Core.Chat.Twitch.PubSub;
using Shinobytes.Core;
using Shinobytes.Network;
using System.Threading.Tasks;

namespace ROBot.API.PacketHandlers
{
    public class PubSubPacketHandler : IServerPacketHandler
    {
        private readonly ILogger logger;
        private readonly IMessageBus messageBus;

        public PubSubPacketHandler(
            ILogger logger,
            IMessageBus messageBus)
        {
            this.logger = logger;
            this.messageBus = messageBus;
        }

        public Task HandleAsync(INetworkClient client, ServerPacket packet)
        {
            if (packet.Data == null || (packet.Data.Buffer?.Length ?? 0) == 0)
            {
                logger.LogError("[LOG] An empty PubSub Packet Recieved");
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

                    //messageBus.Send("pubsub", pubSubRepo.AddOrUpdate(userId, userName, token));
                    //if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(token))
                    //{
                    //    logger.LogError("[LOG] Bad pubsub data recieved: "
                    //        + ", userId: " + userId
                    //        + ", userName: " + userName
                    //        + ", token: " + token
                    //    );
                    //}
                }
            }
            catch (System.Exception exc)
            {
                logger.LogError("[LOG] Bad pubsub data recieved: " + exc
                    + ", userId: " + userId
                    + ", userName: " + userName
                    + ", token: " + token
                    );
            }
            return Task.CompletedTask;
        }
    }
}
