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
        Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, SocketSlashCommand cmd);
        Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, SocketMessage cmd);
        IReadOnlyList<SlashCommandProperties> RegisterSlashCommands(DiscordCommandClient discordCommandClient, DiscordSocketClient discord);
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

        public async Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, SocketSlashCommand command)
        {
            try
            {
                var sender = command.User;
                var channel = command.Channel; // not as interesting if we can reply directly to the command, but for now lets keep this.

                /*
                    We might not be able to find a session using the discord channel. but we can try.
                 */

                var uid = sender.Id;
                var settings = userSettingsManager.Get(uid.ToString(), "discord");
                var cmd = new DiscordCommand(command, settings?.IsAdministrator ?? false, settings?.IsModerator ?? false);

                var argString = !string.IsNullOrEmpty(cmd.Arguments) ? " (args: " + cmd.Arguments + ")" : "";
                var key = cmd.Command.ToLower();

                var session = game.GetSession(cmd.Channel);

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
                logger.LogError("[BOT] Error handling command (Command: " + command?.CommandName + " Exception: " + exc.ToString() + ")");
                return false;
            }
        }

        public async Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, SocketMessage command)
        {
            try
            {
                var sender = command.Author;
                var channel = command.Channel;
                //var msg = command.CleanContent;

                /*
                    We might not be able to find a session using the discord channel. but we can try.
                 */

                var uid = sender.Id;
                var settings = userSettingsManager.Get(uid.ToString(), "discord");
                var cmd = new DiscordCommand(command, settings?.IsAdministrator ?? false, settings?.IsModerator ?? false);

                var argString = !string.IsNullOrEmpty(cmd.Arguments) ? " (args: " + cmd.Arguments + ")" : "";
                var key = cmd.Command.ToLower();

                var session = game.GetSession(cmd.Channel);

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

        public IReadOnlyList<SlashCommandProperties> RegisterSlashCommands(DiscordCommandClient discordCommandClient, DiscordSocketClient discord)
        {
            var cmds = new List<SlashCommandProperties>();
            foreach (var h in handlerLookup)
            {
                var builder = new SlashCommandBuilder();
                builder.Name = h.Key.ToLower();

                var handler = ioc.Resolve(h.Value) as IChatCommandHandler;

                builder.Description = handler.Description ?? h.Key.ToLower();

                if (handler.Inputs != null)
                {
                    foreach (var input in handler.Inputs)
                    {
                        builder.AddOption(input.Name.ToLower(), GetType(input.Type), input.Description, input.IsRequired, options: GetOptions(input.Options), choices: GetChoices(input.Choices));
                    }
                }

                cmds.Add(builder.Build());
            }

            return cmds;
        }

        private List<SlashCommandOptionBuilder> GetOptions(ChatCommandInput[] options)
        {
            var result = new List<SlashCommandOptionBuilder>();

            if (options != null && options.Length > 0)
            {
                foreach (var option in options)
                {
                    var sub = new SlashCommandOptionBuilder();

                    sub.Name = option.Name.ToLower();
                    sub.Description = option.Description ?? option.Name;
                    sub.Type = GetType(option.Type);
                    sub.IsRequired = option.IsRequired;
                    sub.Options = GetOptions(option.Options);
                    sub.Choices = GetChoices(option.Choices).ToList();
                    result.Add(sub);
                }
            }
            return result;
        }

        private ApplicationCommandOptionChoiceProperties[] GetChoices(string[] choices)
        {
            if (choices == null || choices.Length == 0)
                return new ApplicationCommandOptionChoiceProperties[0];
            return choices.Select(x => new ApplicationCommandOptionChoiceProperties { Name = x.ToLower(), Value = x }).ToArray();
        }

        private ApplicationCommandOptionType GetType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return ApplicationCommandOptionType.String;
            }

            var t = type.ToLower();
            if (t == "number")
            {
                return ApplicationCommandOptionType.Number;
            }

            if (t == "user")
            {
                return ApplicationCommandOptionType.User;
            }

            return ApplicationCommandOptionType.String;
        }
    }
}
