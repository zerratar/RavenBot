﻿using Microsoft.Extensions.Logging;
using RavenBot.Core.Ravenfall;
using Shinobytes.Core;
using Shinobytes.Network;
using System.Threading.Tasks;

namespace ROBot.API.PacketHandlers
{
    public class UserSettingsPacketHandler : IServerPacketHandler
    {
        private readonly ILogger logger;
        private readonly IUserSettingsManager settingsManager;
        private readonly IMessageBus messageBus;

        public UserSettingsPacketHandler(
            ILogger logger,
            IUserSettingsManager settingsManager,
            IMessageBus messageBus)
        {
            this.logger = logger;
            this.settingsManager = settingsManager;
            this.messageBus = messageBus;
        }

        public Task HandleAsync(INetworkClient client, ServerPacket packet)
        {
            if (packet.Data == null || (packet.Data.Buffer?.Length ?? 0) == 0)
            {
                logger.LogError("An empty user settings Packet Received");
                return Task.CompletedTask;
            }

            logger.LogDebug("User Settings Packet Received");

            string userId = null;
            string key = null;
            string value = null;
            try
            {
                using (var reader = packet.Data.GetReader())
                {
                    userId = reader.ReadString();
                    key = reader.ReadString();
                    value = reader.ReadString();
                    settingsManager.Set(System.Guid.Parse(userId), key, value);
                }
            }
            catch (System.Exception exc)
            {
                logger.LogError("Bad user settings data received: " + exc
                    + ", userId: " + userId
                    + ", key: " + key
                    + ", key: " + value
                    );
            }
            return Task.CompletedTask;
        }
    }
}
