using System;

namespace ROBot.Core.GameServer
{
    public interface IGameSessionManager
    {
        event EventHandler<IGameSession> SessionStarted;
        event EventHandler<IGameSession> SessionEnded;

        IGameSession Add(IBotServer server, Guid sessionId, string userId, string username, DateTime created);
        void Remove(IGameSession session);

        IGameSession Get(Guid id);
        IGameSession GetByName(string twitchUserName);
        IGameSession GetByUserId(string session);
    }
}