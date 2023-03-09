using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Net;
using Shinobytes.Core;

namespace RavenBot.Core.Handlers
{
    public class TextCommandHandler : ICommandHandler//, ICommandAliasRegister
    {
        private readonly IoC ioc;
        private readonly ILogger logger;
        private readonly ConcurrentDictionary<string, ICommandProcessor> commands
            = new ConcurrentDictionary<string, ICommandProcessor>();

        public TextCommandHandler(IoC ioc)
        {
            this.ioc = ioc;
            this.logger = ioc.Resolve<ILogger>();
        }

        public IReadOnlyDictionary<string, ICommandProcessor> GetCommandProcessors()
        {
            return commands;
        }

        public async Task HandleAsync(IMessageChat listener, ICommand cmd)
        {
            try
            {
                if (string.IsNullOrEmpty(cmd.Command))
                {
                    return;
                }

                if (commands.TryGetValue(cmd.Command, out var processor))
                {
                    var isChannelPointReward = cmd.GetType().Name.Contains("Reward");

                    var isGift = cmd.GetType().Name.Contains("Gift");


                    if (!isChannelPointReward)
                    {
                        if (!string.IsNullOrEmpty(cmd.Arguments))
                        {
                            logger.WriteDebug($"Command received: {cmd.Command} from {cmd.Sender.Username} with args: {cmd.Arguments}");
                        }
                        else
                        {
                            logger.WriteDebug($"Command received: {cmd.Command} from {cmd.Sender.Username}");
                        }
                    }

                    await processor.ProcessAsync(listener, cmd);
                }
            }
            catch (Exception ex)
            {
                logger?.WriteError("Error handling command: " + ex);
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

        public void Register<TCommandProcessor>(string[] cmds)
            where TCommandProcessor : ICommandProcessor
        {
            var key = cmds[0];
            if (cmds.Length > 1)
            {
                Register<TCommandProcessor>(cmds[0], cmds.Where(x => x != key).ToArray());
            }
            else
            {
                Register<TCommandProcessor>(cmds[0]);
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