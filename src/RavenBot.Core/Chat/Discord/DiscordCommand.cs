using Discord.Commands;
using Discord.WebSocket;
using RavenBot.Core.Extensions;
using RavenBot.Core.Handlers;
using System;
using System.Linq;

namespace RavenBot.Core.Chat.Discord
{
    public class DiscordCommand : ICommand
    {
        public DiscordCommand(SocketMessage cmd, bool isGameAdmin = false, bool isGameModerator = false)
        {
            int pos = 0;
            var msg = cmd as SocketUserMessage;
            var msgContent = msg.CleanContent;
            if (msg.HasCharPrefix('!', ref pos))
            {
                msgContent = msgContent.Substring(1);
            }

            msgContent = msgContent.Trim().AsUTF8();
            Command = msgContent.ToLower();
            Arguments = string.Empty;
            if (msgContent.Contains(' '))
            {
                var values = msgContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Command = values[0].Trim();
                Arguments = String.Join(" ", values.Skip(1).ToArray());
            }

            if (!string.IsNullOrEmpty(Arguments) && Arguments.Length > 0)
            {
                var allowedCharacters = "_=qwertyuiopåasdfghj.,-%!\"+$€\\]:;|<>@£½§~¨^´`[klöäzxcvbnm1234567890".ToArray();
                var lastChar = Arguments[Arguments.Length - 1];
                if (!allowedCharacters.Contains(char.ToLower(lastChar)))
                {
                    Arguments = Arguments.Trim(lastChar);
                }

                var firstChar = Arguments[0];
                if (!allowedCharacters.Contains(char.ToLower(firstChar)))
                {
                    Arguments = Arguments.Trim(firstChar);
                }
            }

            Channel = new DiscordChannel(cmd.Channel);

            var author = msg.Author;

            Sender = new DiscordSender(
                author.Id,
                author.Username + "#" + author.Discriminator,
                author.Username,
                isGameAdmin,
                isGameModerator);


            CorrelationId = cmd.Id.ToString();
        }

        public ICommandSender Sender { get; set; }
        public string Command { get; set; }
        public string Arguments { get; set; }
        public ICommandChannel Channel { get; set; }
        public string CorrelationId { get; set; }

        public class DiscordSender : ICommandSender
        {
            public DiscordSender(ulong id, string username, string displayName, bool isGameAdmin, bool isGameModerator)
            {
                UserId = id.ToString();
                this.Username = username;
                this.DisplayName = displayName;
                this.IsGameAdmin = isGameAdmin;
                this.IsGameModerator = isGameModerator;
            }

            public string UserId { get; }

            public string Platform => "discord";

            public string Username { get; }

            public string DisplayName { get; }

            public bool IsGameAdmin { get; }

            public bool IsGameModerator { get; }

            public bool IsBroadcaster { get; }

            public bool IsModerator { get; }

            public bool IsSubscriber { get; }

            public bool IsVip { get; }

            public bool IsVerifiedBot { get; }

            public string ColorHex { get; }
        }

        public class DiscordChannel : ICommandChannel
        {
            public ulong Id { get; }
            public string Name { get; }
            public ISocketMessageChannel Channel { get; }

            public DiscordChannel(ISocketMessageChannel channel)
            {
                Channel = channel;

                Id = channel.Id;
                Name = channel.Name;
            }

            public override string ToString()
            {
                return Channel.ToString();
            }
        }
    }
}