using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RavenBot.Core.Net;

namespace RavenBot.Core.Handlers
{
    public class TextCommandHandler : ICommandHandler//, ICommandAliasRegister
    {
        private readonly IoC ioc;
        private readonly ConcurrentDictionary<string, ICommandProcessor> commands
            = new ConcurrentDictionary<string, ICommandProcessor>();


        public TextCommandHandler(IoC ioc)
        {
            this.ioc = ioc;
        }

        public async Task HandleAsync(IMessageBroadcaster listener, ICommand cmd)
        {
            if (commands.TryGetValue(cmd.Command, out var processor))
            {
                Console.WriteLine($"Command received: {cmd.Command} from {cmd.Sender} with args: {cmd.Arguments}");
                await processor.ProcessAsync(listener, cmd);
            }
        }

        public void Register<TCommandProcessor>(string cmd, params string[] aliases)
            where TCommandProcessor : ICommandProcessor
        {
            ioc.Register<TCommandProcessor>();
            var processor = this.ioc.Resolve<TCommandProcessor>();
            commands[cmd] = processor;

            if (aliases == null || aliases.Length <= 0)
            {
                return;
            }

            foreach (var alias in aliases)
            {
                commands[alias] = processor;
            }
        }
    }

    //public interface ICommandAlias
    //{

    //}

    //public interface ICommandAliasResolver
    //{
    //    // !task hello BECOMES rpg2 task hello

    //    // ICommand Resolve(ICommandAlias alias, string command);        
    //}

    //public interface ICommandAliasRegister
    //{
    //    // !alias fighting = "rpg2 task fighting"
    //    // !alias "task {0}" = "rpg2 task {0}"
        
    //    // ICommandAlias Register(string alias, string source);
    //    // ICommandAlias Update(ICommandAlias alias, string newAlias, string newSource);
    //    // bool UnRegister(ICommandAlias alias);        
    //}
}