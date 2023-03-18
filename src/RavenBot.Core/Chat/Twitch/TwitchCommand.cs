﻿using TwitchLib.Client.Models;
using RavenBot.Core.Handlers;
using System.Linq;
using System.Runtime.CompilerServices;
using RavenBot.Core.Extensions;
using System;

namespace RavenBot.Core.Chat.Twitch
{
    public class TwitchCommand : ICommand
    {
        public TwitchCommand(ChatCommand cmd, bool isGameAdmin = false, bool isGameModerator = false)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            if (cmd.ChatMessage == null) throw new ArgumentException("ChatMessage was null. Unable to parse chat command.", nameof(cmd));

            Command = cmd.CommandText?.ToLower()?.AsUTF8();
            Arguments = cmd.ArgumentsAsString?.AsUTF8();

            if (string.IsNullOrEmpty(Command) && !string.IsNullOrEmpty(Arguments))
            {
                Command = Arguments.Trim();
                if (Command.Contains(' '))
                {
                    var cmdAndArgs = Command.Split(' ');
                    Command = cmdAndArgs[0];
                    Arguments = string.Join(" ", cmdAndArgs.Skip(1));
                }
                else
                {
                    Arguments = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(Arguments) && Arguments.Length > 0)
            {
                var allowedCharacters = "_=qwertyuiopåasdfghj.,-%!\"+$€\\]:;|<>@£½§~¨^´`[klöäzxcvbnm1234567890".ToArray();
                var lastChar = Arguments[Arguments.Length - 1];
                var firstChar = Arguments[0];
                if (!allowedCharacters.Contains(char.ToLower(lastChar)))
                {
                    Arguments = Arguments.Trim(lastChar);
                }

                if (!allowedCharacters.Contains(char.ToLower(firstChar)))
                {
                    Arguments = Arguments.Trim(firstChar);
                }
            }

            Channel = new TwitchChannel(cmd.ChatMessage.Channel);

            var isModerator = cmd.ChatMessage.IsModerator;
            var isSubscriber = cmd.ChatMessage.IsSubscriber;
            var isBroadcaster = cmd.ChatMessage.IsBroadcaster;
            var isVip = cmd.ChatMessage.IsVip;
            var isVerifiedBot = false;

            Sender = new TwitchCommandSender(
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

            CorrelationId = cmd.ChatMessage.Id;
        }

        public string CorrelationId { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FixBadEncoding(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            var encoding = System.Text.Encoding.UTF8;
            var bytes = encoding.GetBytes(message);
            return encoding.GetString(bytes);
        }

        public ICommandChannel Channel { get; }
        public ICommandSender Sender { get; }
        public string Command { get; }
        public string Arguments { get; }
        public override string ToString()
        {
            return (Sender?.Username ?? "???") + ": #" + Channel + ", " + Command + " " + Arguments;
        }
        public class TwitchChannel : ICommandChannel
        {
            public ulong Id => 0;

            public string Name { get; }

            public TwitchChannel(string name)
            {
                Name = name;
            }
            public override string ToString()
            {
                return Name;
            }
        }
        public class TwitchCommandSender : ICommandSender
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
                return Username;
            }
        }
    }
}