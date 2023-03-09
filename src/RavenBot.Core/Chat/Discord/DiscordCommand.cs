using Discord.WebSocket;
using RavenBot.Core.Handlers;
using System;

namespace RavenBot.Core.Chat.Discord
{
    public class DiscordCommand : ICommand
    {
        public DiscordCommand(SocketMessage cmd, bool isGameAdmin = false, bool isGameModerator = false)
        {

        }
        public ICommandSender Sender => throw new NotImplementedException();

        public string Channel => throw new NotImplementedException();

        public string Command => throw new NotImplementedException();

        public string Arguments => throw new NotImplementedException();
    }

}