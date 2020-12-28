using TwitchLib.Client.Models;
using RavenBot.Core.Handlers;
using TwitchLib.Api.Helix.Models.StreamsMetadata;

namespace RavenBot.Core.Twitch
{
    public class TwitchChatMessage
    {
        public TwitchChatMessage(TwitchChatSender sender, TwitchChatMessagePart[] message)
        {
            Sender = sender;
            Message = message;
        }

        public TwitchChatSender Sender { get; }
        public TwitchChatMessagePart[] Message { get; }
    }

    public class TwitchChatMessagePart
    {
        public TwitchChatMessagePart(string type, string value)
        {
            Type = type;
            Value = value;
        }

        public string Type { get; }
        public string Value { get; }
    }

    public class TwitchUserLeft
    {
        public string Name { get; }

        public TwitchUserLeft(string name)
        {
            Name = name;
        }
    }

    public class TwitchUserJoined
    {
        public string Name { get; }

        public TwitchUserJoined(string name)
        {
            Name = name;
        }
    }

    public class TwitchCheer
    {
        public string UserId { get; }
        public string UserName { get; }
        public string DisplayName { get; }
        public bool IsModerator { get; }
        public bool IsSubscriber { get; }
        public bool IsVip { get; }
        public int Bits { get; }

        public TwitchCheer(
            string userId,
            string userName,
            string displayName,
            bool isModerator,
            bool isSubscriber,
            bool isVip,
            int bits)
        {
            UserId = userId;
            UserName = userName;
            IsModerator = isModerator;
            IsSubscriber = isSubscriber;
            IsVip = isVip;
            DisplayName = displayName;
            Bits = bits;
        }
    }

    public class TwitchSubscription
    {
        public string UserId { get; }
        public string ReceiverUserId { get; }
        public bool IsModerator { get; }
        public bool IsSubscriber { get; }
        public string UserName { get; }
        public string DisplayName { get; }
        public int Months { get; }
        public bool IsNew { get; }

        public TwitchSubscription(
            string userId,
            string userName,
            string displayName,
            string receiverUserId,
            bool isModerator,
            bool isSubscriber,
            int months,
            bool isNew)
        {
            UserId = userId;
            ReceiverUserId = receiverUserId;
            IsModerator = isModerator;
            IsSubscriber = isSubscriber;
            UserName = userName;
            DisplayName = displayName;
            Months = months;
            IsNew = isNew;
        }
    }


    public class TwitchChatSender
    {
        public TwitchChatSender(string name, string nameColor)
        {
            Name = name;
            NameColor = nameColor;
        }

        public string Name { get; }
        public string NameColor { get; }
    }
    public class TwitchHypeTrain
    {

    }

    public class TwitchCommand : ICommand
    {
        public TwitchCommand(ChatCommand cmd)
        {
            this.Command = cmd.CommandText?.ToLower();
            this.Arguments = cmd.ArgumentsAsString;

            var isModerator = cmd.ChatMessage.IsModerator;
            var isSubscriber = cmd.ChatMessage.IsSubscriber;
            var isBroadcaster = cmd.ChatMessage.IsBroadcaster;
            var isVip = false;

            this.Sender = new TwitchCommandSender(
                cmd.ChatMessage.UserId,
                cmd.ChatMessage.Username,
                cmd.ChatMessage.DisplayName,
                isBroadcaster,
                isModerator,
                isSubscriber,
                isVip,
                cmd.ChatMessage.ColorHex);
        }

        public ICommandSender Sender { get; }
        public string Command { get; }
        public string Arguments { get; }

        private class TwitchCommandSender : ICommandSender
        {
            public TwitchCommandSender(
                string userId,
                string username,
                string displayName,
                bool isBroadcaster,
                bool isModerator,
                bool isSubscriber,
                bool isVip,
                string colorHex)
            {
                UserId = userId;
                Username = username;
                DisplayName = displayName;
                IsBroadcaster = isBroadcaster;
                IsModerator = isModerator;
                IsSubscriber = isSubscriber;
                IsVip = isVip;
                ColorHex = colorHex;
            }

            public string UserId { get; }
            public string Username { get; }
            public string DisplayName { get; }
            public bool IsBroadcaster { get; }
            public bool IsModerator { get; }
            public bool IsSubscriber { get; }
            public bool IsVip { get; }
            public string ColorHex { get; }

            public override string ToString()
            {
                return this.Username;
            }
        }
    }
}