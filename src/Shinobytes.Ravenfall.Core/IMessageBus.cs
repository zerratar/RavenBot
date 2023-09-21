using System;
using System.Threading.Tasks;

namespace Shinobytes.Core
{
    public interface IMessageBus
    {
        void Send<T>(string key, T message);
        IMessageBusSubscription Subscribe<T>(string key, Action<T> onMessage);
        IMessageBusSubscription Subscribe<T>(string key, Func<T, Task> onMessage);
        IMessageBusSubscription Subscribe(string key, Action onMessage);
    }
}
