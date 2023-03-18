using Newtonsoft.Json;
using System;

namespace RavenBot.Core.Ravenfall.Models
{
    public class User
    {
        public static User ServerRequest { get; } = new User();
        public User()
        {
            Username = "server-request";
            DisplayName = "server-request";
            PlatformId = "server-request";
        }

        public User(
              Guid id,
              Guid characterId,
              string username,
              string displayName,
              string color,
              string platform,
              string platformId,
              bool isBroadcaster,
              bool isModerator,
              bool isSubscriber,
              bool isVip,
              string identifier)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            Id = id;
            CharacterId = characterId;
            Username = username.StartsWith("@") ? username.Substring(1) : username;
            PlatformId = platformId;
            DisplayName = displayName;
            Color = color;
            Platform = platform;
            IsBroadcaster = isBroadcaster;
            IsModerator = isModerator;
            IsSubscriber = isSubscriber;
            IsVip = isVip;
            Identifier = identifier;
        }
        public Guid Id { get; set; }
        public Guid CharacterId { get; set; }
        public string Platform { get; set; }
        public string PlatformId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Color { get; set; }
        public bool IsBroadcaster { get; set; }
        public bool IsModerator { get; set; }
        public bool IsSubscriber { get; set; }
        public bool IsVip { get; set; }
        public int SubTier { get; set; }
        public string Identifier { get; set; }

        [JsonIgnore]
        public UserSettings Settings { get; set; }
    }
}
