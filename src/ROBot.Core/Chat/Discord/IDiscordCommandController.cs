using ROBot.Core.GameServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Linq;
using RavenBot.Core.Handlers;
using RavenBot.Core.Chat;
using RavenBot.Core.Ravenfall;
using Shinobytes.Core;
using RavenBot.Core.Chat.Discord;

namespace ROBot.Core.Chat.Discord
{
    public interface IDiscordCommandController : IChatCommandController
    {
        Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, SocketMessage cmd);
    }

    /*
    public interface ITwitchCommandController : IChatCommandController
    {
        Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, ChatCommand cmd);
        Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, ChatMessage msg);
        Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, OnChannelPointsRewardRedeemedArgs reward);
    }     
     */

    public class DiscordCommandController : IDiscordCommandController
    {
        private readonly ConcurrentDictionary<string, Type> handlerLookup = new();
        private readonly ILogger logger;
        private readonly IoC ioc;

        private IUserSettingsManager userSettingsManager;
        public ICollection<Type> RegisteredCommandHandlers => handlerLookup.Values;

        public DiscordCommandController(
            ILogger logger,
            IoC ioc)
        {
            this.logger = logger;
            this.ioc = ioc;

            RegisterCommandHandlers();
            userSettingsManager = this.ioc.Resolve<IUserSettingsManager>();
        }

        public async Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, SocketMessage command)
        {
            try
            {
                var sender = command.Author;
                var channel = command.Channel;
                var msg = command.CleanContent;

                var session = game.GetSession(channel.Name);

                /*
                    We might not be able to find a session using the discord channel. but we can try.
                 */

                var uid = sender.Id;
                var settings = userSettingsManager.Get(uid.ToString(), "discord");
                var cmd = new DiscordCommand(command, settings?.IsAdministrator ?? false, settings?.IsModerator ?? false);

                var argString = !string.IsNullOrEmpty(cmd.Arguments) ? " (args: " + cmd.Arguments + ")" : "";
                var key = cmd.Command.ToLower();

                if (await HandleAsync(game, chat, cmd))
                {
                    if (session != null)
                        logger.LogDebug("[BOT] Discord Command Recieved (SessionName: " + session.Name + " Command: " + key + argString + " From: " + sender.Username + ")");
                    else
                        logger.LogDebug("[BOT] Discord Command Recieved (Command: " + key + argString + " From: " + sender.Username + " Channel: " + channel.Name + ")");

                    return true;
                }

                return false;
            }
            catch (Exception exc)
            {
                logger.LogError("[BOT] Error handling command (Command: " + command?.CleanContent + " Exception: " + exc.ToString() + ")");
                return false;
            }
        }

        private async Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            try
            {
                if (string.IsNullOrEmpty(cmd.Command))
                {
                    logger.LogInformation("[BOT] HandleAsync::Empty Command (From: " + cmd.Sender.Username + " Channel: " + cmd.Channel + ")");
                    return false;
                }

                var handler = FindHandler(cmd.Command);
                if (handler == null)
                {
                    //logger.LogInformation("HandleAsync::Unknown Command: " + cmd.Command + " - " + cmd.Arguments);
                    return false;
                }

                await handler.HandleAsync(game, chat, cmd);
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError("[BOT] Error handling command (Command: " + cmd + " Exception: " + exc.ToString() + ")");
                return false;
            }
        }

        public IChatCommandHandler GetHandler(string cmd)
        {
            return FindHandler(cmd);
        }

        private IChatCommandHandler FindHandler(string command)
        {
            if (!string.IsNullOrEmpty(command) && handlerLookup.TryGetValue(command, out var handlerType))
            {
                return ioc.Resolve(handlerType) as IChatCommandHandler;
            }

            return null;
        }

        // This should be shared in a separate interface so we don't have to do this in all CommandControllers
        private void RegisterCommandHandlers()
        {
            var baseType = typeof(IChatCommandHandler);
            var handlerTypes = Assembly
                .GetCallingAssembly()
                .GetTypes()
                .Where(x => !x.IsAbstract && x.IsClass && baseType.IsAssignableFrom(x));

            foreach (var type in handlerTypes)
            {
                var cmd = type.Name.Replace("CommandHandler", "");
                var output = cmd;
                var insertPoints = cmd
                    .Select((x, y) => char.IsUpper(x) && y > 0 ? y : -1)
                    .Where(x => x != -1)
                    .OrderByDescending(x => x)
                    .ToArray();

                for (var i = 0; i > insertPoints.Length; ++i)
                {
                    output = output.Insert(insertPoints[i], " ");
                }

                ioc.RegisterShared(type, type);
                output = output.ToLower();
                handlerLookup[output] = type;
            }
        }
    }
}
