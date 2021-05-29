using System;

namespace ROBot.Core.GameServer
{
    public class GameSessionUpdateEventArgs : EventArgs
    {
        public GameSessionUpdateEventArgs(string oldName, string newName)
        {
            OldName = oldName;
            NewName = newName;
        }

        public string OldName { get; }
        public string NewName { get; }
    }
}