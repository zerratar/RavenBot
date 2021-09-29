using System;

namespace ROBot.Core.GameServer
{
    public class GameSessionUpdateEventArgs : EventArgs
    {
        public GameSessionUpdateEventArgs(IGameSession session, string oldName)
        {
            Session = session;
            OldName = oldName;
        }
        public IGameSession Session { get; }
        public string OldName { get; }
    }
}