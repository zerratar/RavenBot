using RavenBot.Core.Handlers;

namespace RavenBot.Core
{
    public interface IMessageChat
    {
        //void Broadcast(string format, params object[] args);
        void SendReply(ICommand cmd, string message, params object[] args);
        void SendReply(string format, object[] args, string correlationId);
        void Announce(string format, params object[] args);
        bool CanRecieveChannelPointRewards { get; }
        //void Send(string target, string message);
    }
}