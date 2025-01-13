using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shinobytes.Core
{
    public class MessageBus : IMessageBus
    {
        public const string Broadcast = "broadcast";
        public const string MessageBusException = "MessageBusException";

        private readonly object mutex = new object();
        private readonly List<Subscription> subscriptions = new List<Subscription>();

        public static MessageBus Instance { get; private set; } = new MessageBus();

        public void Send<T>(string key, T message)
        {
            lock (mutex)
            {
                foreach (var sub in subscriptions.Where(x => x.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)))
                {
                    sub.Invoke(message);
                }
            }
        }

        public IMessageBusSubscription Subscribe(string key, Action onMessage)
        {
            lock (mutex)
            {
                var messageBusSubscription = new Subscription(key, o =>
                {
                    try
                    {
                        onMessage();
                    }
                    catch (Exception exc)
                    {
                        var errMessage = $"MessageBus Callback for '{key}' threw an Exception: " + exc;
                        Console.WriteLine(errMessage);
                        Send(MessageBusException, errMessage);
                    }
                }, this);
                subscriptions.Add(messageBusSubscription);
                return messageBusSubscription;
            }
        }

        public IMessageBusSubscription Subscribe<T>(string key, Action<T> onMessage)
        {
            lock (mutex)
            {
                var messageBusSubscription = new Subscription(key, o =>
                {
                    try
                    {
                        onMessage((T)o);
                    }
                    catch (Exception exc)
                    {
                        var errMessage = $"MessageBus Callback for '{key}' threw an Exception: " + exc;
                        Console.WriteLine(errMessage);
                        Send(MessageBusException, errMessage);
                    }
                }, this);
                subscriptions.Add(messageBusSubscription);
                return messageBusSubscription;
            }
        }

        public IMessageBusSubscription Subscribe<T>(string key, Func<T, Task> onMessage)
        {
            lock (mutex)
            {
                var messageBusSubscription = new Subscription(key, async o =>
                {
                    try
                    {
                        await onMessage((T)o);
                    }
                    catch (Exception exc)
                    {
                        var errMessage = $"MessageBus Callback for '{key}' threw an Exception: " + exc;
                        Console.WriteLine(errMessage);
                        Send(MessageBusException, errMessage);
                    }
                }, this);
                subscriptions.Add(messageBusSubscription);
                return messageBusSubscription;
            }
        }

        private void Unsubscribe(IMessageBusSubscription subscription)
        {
            lock (mutex)
            {
                subscriptions.Remove(subscription as Subscription);
            }
        }

        private class Subscription : IMessageBusSubscription
        {
            private readonly Action<object> onMessage;
            private readonly MessageBus bus;

            public Subscription(string key, Action<object> onMessage, MessageBus bus)
            {
                Key = key;
                this.onMessage = onMessage;
                this.bus = bus;
            }

            public string Key { get; }

            public void Invoke<T>(T message)
            {
                onMessage?.Invoke(message);
            }

            public void Unsubscribe()
            {
                bus.Unsubscribe(this);
            }
        }
    }
}
