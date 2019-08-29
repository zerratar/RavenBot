using System.Threading.Tasks;
using RavenBot.Core.Net;

namespace RavenBot.Core.Handlers
{
    public interface ICommandHandler
    {
        Task HandleAsync(IMessageBroadcaster listener, ICommand cmd);
        void Register<TCommandProcessor>(string cmd, params string[] aliases) 
            where TCommandProcessor : ICommandProcessor;        
    }
}