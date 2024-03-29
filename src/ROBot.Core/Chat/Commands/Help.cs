﻿using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Help : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "Gets a list of commands that can be used in the game";

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            await chat.SendReplyAsync(cmd, "Please see all available game commands at https://www.ravenfall.stream/commands");
        }
    }
}
