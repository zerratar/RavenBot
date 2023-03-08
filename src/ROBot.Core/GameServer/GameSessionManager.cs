using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Ravenfall.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ROBot.Core.GameServer
{
    public class GameSessionManager : IGameSessionManager
    {
        private readonly List<IGameSession> sessions = new List<IGameSession>();
        private readonly object sessionMutex = new object();
        private readonly IUserSettingsManager userSettingsManager;

        public event EventHandler<IGameSession> SessionStarted;
        public event EventHandler<IGameSession> SessionEnded;
        public event EventHandler<GameSessionUpdateEventArgs> SessionUpdated;

        public GameSessionManager(IUserSettingsManager userSettingsManager)
        {
            this.userSettingsManager = userSettingsManager;
        }

        public IGameSession Add(IBotServer server, Guid sessionId, Guid userId, User owner, DateTime created)
        {
            lock (sessionMutex)
            {
                var existingSession = sessions.FirstOrDefault(x => x.Id == sessionId);
                if (existingSession != null)
                {
                    if (SessionStarted != null)
                        SessionStarted.Invoke(this, existingSession);

                    return existingSession;
                }

                var session = new RavenfallGameSession(server, userSettingsManager, sessionId, userId, owner, created);
                sessions.Add(session);
                if (SessionStarted != null)
                    SessionStarted.Invoke(this, session);
                return session;
            }
        }

        public void Update(Guid sessionId, Guid userId, User newOwner)
        {
            var session = Get(sessionId);
            var oldName = session.Name;
            session.RavenfallUserId = userId;
            session.Owner = newOwner;
            session.Name = newOwner.Username;
            SessionUpdated?.Invoke(this, new GameSessionUpdateEventArgs(session, oldName));
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

        public void ClearAll()
        {
            lock (sessionMutex)
            {
                var s = sessions.ToList();
                foreach (var session in s)
                {
                    Remove(session);
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


        public IGameSession GetByUserId(Guid ravenfallUserId)
        {
            lock (sessionMutex)
            {
                return sessions.FirstOrDefault(x => x.RavenfallUserId == ravenfallUserId);
            }
        }

        public IReadOnlyList<IGameSession> All()
        {
            lock (sessionMutex)
            {
                return sessions.ToList();
            }
        }

    }
}