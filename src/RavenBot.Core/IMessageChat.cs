namespace RavenBot.Core
{
    public interface IMessageChat
    {
        void Broadcast(string format, params object[] args);
        void Broadcast(string user, string format, params object[] args);
        //void Send(string target, string message);
    }
}