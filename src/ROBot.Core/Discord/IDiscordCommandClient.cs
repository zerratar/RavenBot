using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ROBot.Core.Discord
{
    public interface IDiscordCommandClient : IDisposable
    {
        void Start();
        void Stop();
        void Broadcast(IGameSessionCommand message);
        void Broadcast(string channel, string user, string format, params object[] args);
        void SendChatMessage(string channel, string message);
    }
}
