using RavenBot.Core.Handlers;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Chat
{
    public interface IChatCommandClient : IDisposable
    {
        Task StartAsync();
        void Stop();

        Task BroadcastAsync(SessionGameMessageResponse message);
        Task SendReplyAsync(ICommand command, string format, params object[] args);
        Task SendMessageAsync(ICommandChannel channel, string format, object[] args);
    }
}
