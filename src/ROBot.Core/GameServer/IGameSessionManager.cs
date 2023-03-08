using RavenBot.Core.Ravenfall.Models;
using System;
using System.Collections.Generic;

namespace ROBot.Core.GameServer
{

    public interface IGameSessionManager
    {
        event EventHandler<IGameSession> SessionStarted;
        event EventHandler<IGameSession> SessionEnded;
        event EventHandler<GameSessionUpdateEventArgs> SessionUpdated;
        IGameSession Add(IBotServer server, Guid sessionId, Guid ravenfallUserId, Player owner, DateTime created);
        void Remove(IGameSession session);
        void Update(Guid sessionId, Guid ravenfallUserId, Player owner);
        IReadOnlyList<IGameSession> All();
        IGameSession Get(Guid id);
        IGameSession GetByName(string name);
        IGameSession GetByUserId(Guid ravenfallUserId);
        void ClearAll();
    }
}