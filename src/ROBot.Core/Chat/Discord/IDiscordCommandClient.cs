using ROBot.Core.Chat;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ROBot.Core.Chat.Discord
{
    public interface IDiscordCommandClient : IChatCommandClient
    {
        void SessionEnded(IGameSession session);
        void SessionStarted(IGameSession session);
    }
}
