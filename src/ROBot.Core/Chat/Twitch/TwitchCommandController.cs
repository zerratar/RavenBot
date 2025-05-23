﻿using Microsoft.Extensions.Logging;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using Shinobytes.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Chat.Twitch
{
    public class TwitchCommandController : ITwitchCommandController
    {
        private const string ChatMessageHandlerName = "MessageHandler";
        private readonly ILogger logger;
        //private readonly IUserProvider userProvider;
        private readonly IoC ioc;

        private readonly ConcurrentDictionary<string, Type> handlerLookup = new ConcurrentDictionary<string, Type>();

        private ITwitchChatMessageHandler messageHandler;
        private IUserSettingsManager userSettingsManager;

        public ICollection<Type> RegisteredCommandHandlers => handlerLookup.Values;

        public TwitchCommandController(
            ILogger logger,
            //IUserProvider userProvider,
            IoC ioc)
        {
            this.logger = logger;
            //this.userProvider = userProvider;
            this.ioc = ioc;

            RegisterCommandHandlers();
            userSettingsManager = this.ioc.Resolve<IUserSettingsManager>();
        }

        public async Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, TwitchLib.Client.Models.ChatMessage message)
        {
            try
            {
                if (message == null || string.IsNullOrEmpty(message.Message) || message.Message.StartsWith("!"))
                    return true;

                if (messageHandler == null)
                    messageHandler = ioc.Resolve<ITwitchChatMessageHandler>();

                if (messageHandler == null)
                {
                    logger.LogInformation("[BOT] HandleMessage: No message handler available.");
                    return false;
                }

                await messageHandler.HandleAsync(game, chat as ITwitchCommandClient, message);
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError("Error handling command: " + exc.ToString());
                return false;
            }
        }

        public async Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, TwitchLib.Client.Models.ChatCommand command, TwitchLib.Client.Models.ChatMessage msg)
        {
            try
            {
                if (command == null || string.IsNullOrEmpty(command.CommandText))
                {
                    if (command != null && msg != null)
                    {
                        logger.LogError("[BOT] Error handling command. Command is null. Message: " + msg.Username + ": " + msg.Message + " @" + msg.Channel);
                    }
                    else
                    {
                        logger.LogError("[BOT] Error handling command. Command is null.");
                    }
                    return false;
                }
                var argString = !string.IsNullOrEmpty(command.ArgumentsAsString) ? " (args: " + command.ArgumentsAsString + ")" : "";

                var uid = msg.UserId;
                var settings = userSettingsManager.Get(uid, "twitch");
                var cmd = new TwitchCommand(command, msg, settings?.IsAdministrator ?? false, settings?.IsModerator ?? false);

                var session = game.GetSession(cmd.Channel);

                var key = cmd.Command.ToLower();

                if (session != null)
                {
                    // add the user as part of this session
                    session.Get(cmd);
                }

                if (await HandleAsync(game, chat as ITwitchCommandClient, cmd))
                {
                    if (session != null)
                        logger.LogDebug("[BOT] Twitch Command Recieved (SessionName: " + session.Name + " Command: " + key + argString + " From: " + msg.Username + ")");
                    else
                        logger.LogDebug("[BOT] Twitch Command Recieved (Command: " + key + argString + " From: " + msg.Username + " Channel: " + msg.Channel + ")");

                    return true;
                }

                return false;
            }
            catch (Exception exc)
            {
                logger.LogError("[BOT] Error handling command (Command: " + command?.CommandText + " Exception: " + exc.ToString() + ")");
                return false;
            }
        }

        public async Task<bool> HandleAsync(IBotServer game, IChatCommandClient twitch, OnChannelPointsRewardRedeemedArgs reward)
        {
            //logger.LogInformation("Channel Point Rewards not implemented.");
            var cmd = "";
            var usedCommand = "";
            try
            {
                var redeemer = reward.RewardRedeemed.Redemption.User;

                uint.TryParse(reward.ChannelId, out var channelId);

                // Todo: Test to see if Prompt contains a valid username before setting arguments.
                // Prompt also seem to be the channel reward description when set
                //var arguments = reward.RewardRedeemed.Redemption.Reward.Prompt?.Trim();
                var arguments = redeemer.Login; //Will ignore anything from twitch
                var command = reward.RewardRedeemed.Redemption.Reward.Title;
                var cmdParts = command.ToLower().Split(' ');

                //var channelUser = userProvider.GetByUserId(channelId.ToString());
                //channelUser?.Username ?? 

                var settings = userSettingsManager.GetAll();
                var s = settings.FirstOrDefault(x => x.TwitchUserId != null && x.TwitchUserId == reward.ChannelId);
                var channel = new TwitchCommand.TwitchChannel(channelId, s?.TwitchUserName ?? arguments);
                var session = game.GetSession(channel);
                if (session == null)
                {
                    logger.LogError("Handle Channel Point Reward Failed: Session could not be found matching Twitch User Id: " + channelId + ", Ravenfall disconnected? Command: " + command + ", argument: " + arguments);
                    return false;
                }

                // In case we use brackets to identify a command
                cmd = cmdParts.FirstOrDefault(x => x.Contains("["));
                usedCommand = cmd;

                if (!string.IsNullOrEmpty(cmd))
                    cmd = cmd.Replace("[", "").Replace("]", "").Trim();

                IChatCommandHandler processor = null;

                // if we did not use any brackets
                if (string.IsNullOrEmpty(cmd))
                {
                    foreach (var part in cmdParts)
                    {
                        var proc = FindHandler(part.ToLower());
                        if (proc != null)
                        {
                            processor = proc;
                            usedCommand = part;
                            break;
                        }
                    }
                }

                // in case we never found a handler, fallback to identifier
                if (processor == null)
                {
                    cmd = cmd?.ToLower();
                    if (string.IsNullOrEmpty(cmd))
                    {
                        return false;
                    }

                    processor = FindHandler(cmd);
                    if (processor == null)
                    {
                        logger.LogDebug("[BOT] Unknown Reward - No Handler Found (Command: " + cmd + ")");
                        //Not an Error, expected to sometimes see rewards unrelated to Ravenfall
                        return false;
                    }

                    usedCommand = cmd;
                }

                if (string.IsNullOrEmpty(arguments))
                {
                    arguments = redeemer.Login;
                }

                //if (processor.RequiresBroadcaster)
                //{
                //    return new RewardRedeemCommand(broadcaster, usedCommand, redeemer.Username);
                //}
                //else
                //{
                //    return new RewardRedeemCommand(redeemer, usedCommand, arguments);
                //}

                RavenBot.Core.Ravenfall.Models.User player = null;
                if (processor.RequiresBroadcaster)
                {
                    player = session.GetBroadcaster();
                }
                else
                {
                    player = session.Get(new TwitchRewardRedeemUser(redeemer));
                    if (player == null)
                    {
                        logger.LogError("[BOT] Error Redeeming Reward - Redeemer Does not Exisit (Command: " + usedCommand + " Redeemer: " + redeemer.Id + ")");
                        return false;
                    }
                }

                logger.LogDebug("[TWITCH] Reward Redeemed (Command: " + usedCommand + " | Redeemer: " + player.Username + " | Channel: " + session.Name + " | Arguments: " + arguments + " | Prompt: " + reward.RewardRedeemed.Redemption.Reward.Prompt?.Trim() + ")");
                await processor.HandleAsync(game, twitch, new RewardRedeemCommand(player, session.Name, usedCommand, arguments));
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError("[BOT] Exception Redeeming Reward  (Command: " + usedCommand + " Exception: " + exc.ToString() + ")");
                return false;
            }
        }

        public IChatCommandHandler GetHandler(string cmd)
        {
            return FindHandler(cmd);
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
            ioc.RegisterShared<ITwitchChatMessageHandler, TwitchChatMessageHandler>();

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

        internal class TwitchRewardRedeemUser : ICommandSender
        {
            private readonly TwitchLib.PubSub.Models.Responses.Messages.User user;

            public TwitchRewardRedeemUser(TwitchLib.PubSub.Models.Responses.Messages.User user)
            {
                this.user = user;
            }

            public string UserId => user.Id;
            public string Platform => "twitch";
            public string Username => user.Login;

            public string DisplayName => user.DisplayName;

            public bool IsBroadcaster => false;

            public bool IsModerator => false;

            public bool IsSubscriber => false;

            public bool IsVip => false;

            public string ColorHex => "#ffffff";

            public bool IsVerifiedBot => false;

            public bool IsGameAdmin => false;

            public bool IsGameModerator => false;
        }
    }
}