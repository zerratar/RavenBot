using System.Collections.Generic;
using System.Threading.Tasks;
using RavenBot.Core.Net;

namespace RavenBot.Core.Handlers
{
    public interface ICommandHandler
    {
        Task HandleAsync(IMessageChat listener, ICommand cmd);
        void Register<TCommandProcessor>(string cmd, params string[] aliases)
            where TCommandProcessor : ICommandProcessor;
        void Register<TCommandProcessor>(string[] cmds)
            where TCommandProcessor : ICommandProcessor;
        IReadOnlyDictionary<string, ICommandProcessor> GetCommandProcessors();
    }
}