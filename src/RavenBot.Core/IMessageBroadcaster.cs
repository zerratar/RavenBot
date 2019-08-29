namespace RavenBot.Core
{
    public interface IMessageBroadcaster
    {
        void Broadcast(string message);
        void Send(string target, string message);
    }
}