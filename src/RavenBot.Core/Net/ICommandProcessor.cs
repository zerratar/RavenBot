using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Net
{
    public abstract class CommandProcessor : ICommandProcessor
    {
        public virtual void Dispose()
        {
        }

        public abstract Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd);
    }

    public interface ICommandProcessor : IDisposable
    {
        Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd);
    }
}