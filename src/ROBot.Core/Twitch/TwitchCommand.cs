﻿
using RavenBot.Core.Net;

namespace ROBot.Core.Twitch
{

    public interface IGameSessionCommand : IGameCommand
    {
        IGameSession Session { get; }
    }

    public class GameSessionCommand : IGameSessionCommand
    {
        public GameSessionCommand(IGameSession session, string destination, string id, string message, params string[] args)
        {
            this.Session = session;
            this.Receiver = destination;
            this.Identifier = id;
            this.Format = message;
            this.Args = args;
        }
        public IGameSession Session { get; }
        public string Receiver { get; }
        public string Identifier { get; }
        public string Format { get; }
        public string[] Args { get; }
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
        public string Channel { get; }
        public string UserId { get; }
        public string UserName { get; }
        public string DisplayName { get; }
        public bool IsModerator { get; }
        public bool IsSubscriber { get; }
        public bool IsVip { get; }
        public int Bits { get; }

        public TwitchCheer(
            string channel,
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
        public string Channel { get; }
        public string UserId { get; }
        public string ReceiverUserId { get; }
        public bool IsModerator { get; }
        public bool IsSubscriber { get; }
        public string UserName { get; }
        public string DisplayName { get; }
        public int Months { get; }
        public bool IsNew { get; }

        public TwitchSubscription(
            string channel,
            string userId,
            string userName,
            string displayName,
            string receiverUserId,
            bool isModerator,
            bool isSubscriber,
            int months,
            bool isNew)
        {
            Channel = channel;
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

}