using RavenBot.Core.Handlers;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Chat
{
    public interface IChatCommandClient : IDisposable
    {
        Task StartAsync();
        void Stop();

        void Broadcast(SessionGameMessageResponse message);
        void SendReply(ICommand command, string format, params object[] args);
        void SendMessage(ICommandChannel channel, string format, object[] args);
    }
}
