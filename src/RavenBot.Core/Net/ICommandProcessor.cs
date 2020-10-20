using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Net
{
    public abstract class RavenfallCommandProcessor : ICommandProcessor
    {
        public virtual void Dispose()
        {
        }

        public abstract Task ProcessAsync(IMessageChat broadcaster, ICommand cmd);
    }

    public interface ICommandProcessor : IDisposable
    {
        Task ProcessAsync(IMessageChat broadcaster, ICommand cmd);
    }
}