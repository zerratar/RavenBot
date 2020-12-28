using System;

namespace RavenBot.Core.Ravenfall.Models
{
    public class Player
    {
        public Player()
        {
        }

        public Player(
              string userId,
              string username,
              string displayName,
              string color,
              bool isBroadcaster,
              bool isModerator,
              bool isSubscriber,
              bool isVip,
              string identifier)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            Username = username.StartsWith("@") ? username.Substring(1) : username;
            UserId = userId;
            DisplayName = displayName;
            Color = color;
            IsBroadcaster = isBroadcaster;
            IsModerator = isModerator;
            IsSubscriber = isSubscriber;
            IsVip = isVip;
            Identifier = identifier;
        }

        public string Username { get; set; }
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string Color { get; set; }
        public bool IsBroadcaster { get; set; }
        public bool IsModerator { get; set; }
        public bool IsSubscriber { get; set; }
        public bool IsVip { get; set; }
        public int SubTier { get; set; }
        public string Identifier { get; set; }
    }
}
