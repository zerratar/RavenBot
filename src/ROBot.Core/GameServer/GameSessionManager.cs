﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ROBot.Core.GameServer
{
    public class GameSessionManager : IGameSessionManager
    {
        private readonly List<IGameSession> sessions = new List<IGameSession>();
        private readonly object sessionMutex = new object();
        public event EventHandler<IGameSession> SessionStarted;
        public event EventHandler<IGameSession> SessionEnded;

        public IGameSession Add(IBotServer server, Guid sessionId, string userId, string username)
        {
            lock (sessionMutex)
            {
                var session = new RavenfallGameSession(server, sessionId, userId, username);
                sessions.Add(session);
                if (SessionStarted != null)
                    SessionStarted.Invoke(this, session);
                return session;
            }
        }

        public void Remove(IGameSession session)
        {
            if (session == null)
                return;

            lock (sessionMutex)
            {
                if (sessions.Remove(session))
                {
                    if (SessionEnded != null)
                        SessionEnded.Invoke(this, session);
                }
            }
        }

        public IGameSession Get(Guid id)
        {
            lock (sessionMutex)
            {
                return sessions.FirstOrDefault(x => x.Id == id);
            }
        }

        public IGameSession GetByName(string twitchUserName)
        {
            lock (sessionMutex)
            {
                return sessions.FirstOrDefault(x => x.Name.Equals(twitchUserName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}