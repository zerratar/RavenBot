using System;
using System.Collections.Generic;

namespace ROBot.Core.GameServer
{

    public interface IGameSessionManager
    {
        event EventHandler<IGameSession> SessionStarted;
        event EventHandler<IGameSession> SessionEnded;
        event EventHandler<GameSessionUpdateEventArgs> SessionUpdated;
        IGameSession Add(IBotServer server, Guid sessionId, string userId, string username, DateTime created);
        void Remove(IGameSession session);
        void UpdateName(Guid sessionId, string newSessionName);
        IReadOnlyList<IGameSession> All();
        IGameSession Get(Guid id);
        IGameSession GetByName(string twitchUserName);
        IGameSession GetByUserId(string session);
    }
}