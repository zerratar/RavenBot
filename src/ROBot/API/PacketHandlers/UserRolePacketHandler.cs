using Microsoft.Extensions.Logging;
using RavenBot.Core.Ravenfall.Commands;
using ROBot.Core.Twitch;
using Shinobytes.Network;
using Shinobytes.Ravenfall.RavenNet.Core;
using System.Threading.Tasks;

namespace ROBot.LogServer.PacketHandlers
{
    public class UserRolePacketHandler : IServerPacketHandler
    {
        private readonly ILogger logger;
        private readonly IUserRoleManager roleManager;
        private readonly IMessageBus messageBus;

        public UserRolePacketHandler(
            ILogger logger,
            IUserRoleManager roleManager,
            IMessageBus messageBus)
        {
            this.logger = logger;
            this.roleManager = roleManager;
            this.messageBus = messageBus;
        }

        public Task HandleAsync(INetworkClient client, ServerPacket packet)
        {
            if (packet.Data == null || (packet.Data.Buffer?.Length ?? 0) == 0)
            {
                this.logger.LogError("An empty user role Packet Received");
                return Task.CompletedTask;
            }

            this.logger.LogDebug("User Role Packet Received");

            string userId = null;
            string userName = null;
            string role = null;
            try
            {
                using (var reader = packet.Data.GetReader())
                {
                    userId = reader.ReadString();
                    userName = reader.ReadString();
                    role = reader.ReadString();

                    roleManager.SetRole(userId, role);

                    //messageBus.Send("user-role", pubSubRepo.AddOrUpdate(userId, userName, role));
                    //if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(role))
                    //{
                    //    this.logger.LogError("Bad pubsub data received: "
                    //        + ", userId: " + userId
                    //        + ", userName: " + userName
                    //        + ", token: " + role
                    //    );
                    //}
                }
            }
            catch (System.Exception exc)
            {
                this.logger.LogError("Bad user role data received: " + exc
                    + ", userId: " + userId
                    + ", userName: " + userName
                    + ", role: " + role
                    );
            }
            return Task.CompletedTask;
        }
    }
}
