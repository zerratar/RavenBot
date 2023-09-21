using RavenBot.Core.Handlers;
using System.Threading.Tasks;

namespace RavenBot.Core
{
    public interface IMessageChat
    {
        //void Broadcast(string format, params object[] args);
        Task SendReplyAsync(ICommand cmd, string message, params object[] args);
        Task SendReplyAsync(string format, object[] args, string correlationId, string mention);
        Task AnnounceAsync(string format, params object[] args);
        bool CanRecieveChannelPointRewards { get; }
        //void Send(string target, string message);
    }
}