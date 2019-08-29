using System;

namespace RavenBot.Core.Ravenfall.Models
{
    public class Player
    { public Player(
            string userId,
            string username,
            string displayName,
            string color,
            bool isBroadcaster,
            bool isModerator,
            bool isSubscriber)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            Username = username.StartsWith("@") ? username.Substring(1) : username;
            UserId = userId;
            DisplayName = displayName;
            Color = color;
            IsBroadcaster = isBroadcaster;
            IsModerator = isModerator;
            IsSubscriber = isSubscriber;
        }

        public string Username { get; }
        public string UserId { get; }
        public string DisplayName { get; }
        public string Color { get; }
        public bool IsBroadcaster { get; }
        public bool IsModerator { get; }
        public bool IsSubscriber { get; }
    }
}
