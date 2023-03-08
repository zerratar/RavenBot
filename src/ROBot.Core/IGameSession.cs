﻿using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System;

namespace ROBot.Core
{
    public interface IGameSession
    {
        Guid Id { get; }
        Guid RavenfallUserId { get; set; }
        Player Owner { get; set; }
        string Name { get; set; }
        public DateTime Created { get; }
        Player GetBroadcaster();
        Player Get(string twitchId);
        Player Get(ICommandSender user);
        Player GetUserByName(string username);
        Player Join(ICommandSender user, string identifier = "1");
        int UserCount { get; }
        bool Contains(string userId);
        bool ContainsUsername(string username);
        void Leave(string userId);
        void SendChatMessage(string username, string message);
    }
}
