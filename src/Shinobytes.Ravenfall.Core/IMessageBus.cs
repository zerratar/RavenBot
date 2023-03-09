using System;

namespace Shinobytes.Core
{
    public interface IMessageBus
    {
        void Send<T>(string key, T message);
        IMessageBusSubscription Subscribe<T>(string key, Action<T> onMessage);
        IMessageBusSubscription Subscribe(string key, Action onMessage);
    }
}
