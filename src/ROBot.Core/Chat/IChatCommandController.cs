using System;
using System.Collections.Generic;

namespace ROBot.Core.Chat
{
    public interface IChatCommandController
    {
        ICollection<Type> RegisteredCommandHandlers { get; }
        IChatCommandHandler GetHandler(string cmd);
    }

}
