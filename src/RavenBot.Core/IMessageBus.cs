using System;

namespace RavenBot.Core
{
    public interface IMessageBus
    {
        void Send<T>(string key, T message);
        IMessageBusSubscription Subscribe<T>(string key, Action<T> onMessage);
    }

    public interface IMessageBusSubscription
    {
        void Unsubscribe();
    }
}
