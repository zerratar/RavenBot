using System;
using System.Threading.Tasks;

namespace ROBot.Core.Chat
{
    public interface IChatCommandClient : IDisposable
    {
        void Start();
        void Stop();

        void Broadcast(IGameSessionCommand message);
        void Broadcast(string channel, string user, string format, params object[] args);
        Task SendChatMessageAsync(string channel, string message);
    }
}
