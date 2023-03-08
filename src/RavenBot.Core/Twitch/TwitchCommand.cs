using TwitchLib.Client.Models;
using RavenBot.Core.Handlers;
using System.Linq;
using System.Runtime.CompilerServices;
using RavenBot.Core.Extensions;
using System;

namespace RavenBot.Core.Twitch
{
    public class ChatMessage
    {
        public ChatMessage(ChatSender sender, ChatMessagePart[] message)
        {
            Sender = sender;
            Message = message;
        }

        public ChatSender Sender { get; }
        public ChatMessagePart[] Message { get; }
    }

    public class ChatMessagePart
    {
        public ChatMessagePart(string type, string value)
        {
            Type = type;
            Value = value;
        }

        public string Type { get; }
        public string Value { get; }
    }

    public class UserLeftEvent
    {
        public string Name { get; }

        public UserLeftEvent(string name)
        {
            Name = name;
        }
    }

    public class UserJoinedEvent
    {
        public string Name { get; }

        public UserJoinedEvent(string name)
        {
            Name = name;
        }
    }

    public class CheerBitsEvent
    {
        public string Channel { get; }
        public string UserId { get; }
        public string Platform { get; }
        public string UserName { get; }
        public string DisplayName { get; }
        public bool IsModerator { get; }
        public bool IsSubscriber { get; }
        public bool IsVip { get; }
        public int Bits { get; }

        public CheerBitsEvent(
            string platform,
            string channel,
            string userId,
            string userName,
            string displayName,
            bool isModerator,
            bool isSubscriber,
            bool isVip,
            int bits)
        {
            Platform = platform;
            Channel = channel;
            UserId = userId;
            UserName = userName;
            IsModerator = isModerator;
            IsSubscriber = isSubscriber;
            IsVip = isVip;
            DisplayName = displayName;
            Bits = bits;
        }
    }

    public class UserSubscriptionEvent
    {
        public string Channel { get; }
        public string UserId { get; }
        public string Platform { get; }
        public string ReceiverUserId { get; }
        public bool IsModerator { get; }
        public bool IsSubscriber { get; }
        public string UserName { get; }
        public string DisplayName { get; }
        public int Months { get; }
        public bool IsNew { get; }

        public UserSubscriptionEvent(
            string platform,
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
            Platform = platform;
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


    public class ChatSender
    {
        public ChatSender(string name, string nameColor)
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

        public TwitchCommand(ChatCommand cmd, bool isGameAdmin = false, bool isGameModerator = false)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            if (cmd.ChatMessage == null) throw new ArgumentException("ChatMessage was null. Unable to parse chat command.", nameof(cmd));

            this.Command = cmd.CommandText?.ToLower()?.AsUTF8();
            this.Arguments = cmd.ArgumentsAsString?.AsUTF8();

            if (string.IsNullOrEmpty(this.Command) && !string.IsNullOrEmpty(this.Arguments))
            {
                this.Command = this.Arguments.Trim();
                if (this.Command.Contains(' '))
                {
                    var cmdAndArgs = this.Command.Split(' ');
                    this.Command = cmdAndArgs[0];
                    this.Arguments = string.Join(" ", cmdAndArgs.Skip(1));
                }
                else
                {
                    this.Arguments = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(this.Arguments) && this.Arguments.Length > 0)
            {
                var allowedCharacters = "_=qwertyuiopåasdfghj.,-%!\"+$€\\]:;|<>@£½§~¨^´`[klöäzxcvbnm1234567890".ToArray();
                var lastChar = this.Arguments[this.Arguments.Length - 1];
                var firstChar = this.Arguments[0];
                if (!allowedCharacters.Contains(char.ToLower(lastChar)))
                {
                    this.Arguments = this.Arguments.Trim(lastChar);
                }

                if (!allowedCharacters.Contains(char.ToLower(firstChar)))
                {
                    this.Arguments = this.Arguments.Trim(firstChar);
                }
            }

            this.Channel = cmd.ChatMessage.Channel;

            var isModerator = cmd.ChatMessage.IsModerator;
            var isSubscriber = cmd.ChatMessage.IsSubscriber;
            var isBroadcaster = cmd.ChatMessage.IsBroadcaster;
            var isVip = false;
            var isVerifiedBot = false;
            this.Sender = new TwitchCommandSender(
                cmd.ChatMessage.UserId,
                cmd.ChatMessage.Username,
                cmd.ChatMessage.DisplayName,
                isGameAdmin,
                isGameModerator,
                isBroadcaster,
                isModerator,
                isSubscriber,
                isVip,
                isVerifiedBot,
                cmd.ChatMessage.ColorHex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FixBadEncoding(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            var encoding = System.Text.Encoding.UTF8;
            var bytes = encoding.GetBytes(message);
            return encoding.GetString(bytes);
        }

        public string Channel { get; }
        public ICommandSender Sender { get; }
        public string Command { get; }
        public string Arguments { get; }
        public override string ToString()
        {
            return (Sender?.Username ?? "???") + ": #" + Channel + ", " + Command + " " + Arguments;
        }

        private class TwitchCommandSender : ICommandSender
        {
            public TwitchCommandSender(
                string userId,
                string username,
                string displayName,
                bool isGameAdmin,
                bool isGameModerator,
                bool isBroadcaster,
                bool isModerator,
                bool isSubscriber,
                bool isVip,
                bool isVerifiedBot,
                string colorHex)
            {
                UserId = userId;
                Username = username;
                DisplayName = displayName;
                IsGameAdmin = isGameAdmin;
                IsGameModerator = isGameModerator;
                IsBroadcaster = isBroadcaster;
                IsModerator = isModerator;
                IsSubscriber = isSubscriber;
                IsVip = isVip;
                ColorHex = colorHex;
                IsVerifiedBot = isVerifiedBot;
            }
            public string Platform => "twitch";
            public string UserId { get; }
            public string Username { get; }
            public string DisplayName { get; }
            public bool IsBroadcaster { get; }
            public bool IsModerator { get; }
            public bool IsSubscriber { get; }
            public bool IsVip { get; }
            public bool IsVerifiedBot { get; }
            public string ColorHex { get; }
            public bool IsGameAdmin { get; }
            public bool IsGameModerator { get; }
            public override string ToString()
            {
                return this.Username;
            }
        }
    }
}