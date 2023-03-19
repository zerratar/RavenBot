using System;

namespace RavenBot.Core.Net
{
    public class GameMessageRecipent
    {
        public GameMessageRecipent(Guid userId, Guid characterId, string platform, string platformId, string platformUserName)
        {
            UserId = userId;
            CharacterId = characterId;
            Platform = platform;
            PlatformId = platformId;
            PlatformUserName = platformUserName;
        }

        public Guid UserId { get; }
        public Guid CharacterId { get; }
        public string Platform { get; }
        public string PlatformId { get; }
        public string PlatformUserName { get; }
    }
}