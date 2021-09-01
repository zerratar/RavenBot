using Microsoft.Extensions.Logging;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Twitch;
using ROBot.Core.GameServer;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Twitch
{
    public class TwitchCommandController : ITwitchCommandController
    {
        private const string ChatMessageHandlerName = "MessageHandler";
        private readonly ILogger logger;
        private readonly IoC ioc;

        private readonly ConcurrentDictionary<string, Type> handlerLookup = new ConcurrentDictionary<string, Type>();

        private ITwitchChatMessageHandler messageHandler;
        private IUserRoleManager userRoleManager;

        public TwitchCommandController(
            ILogger logger,
            IoC ioc)
        {
            this.logger = logger;
            this.ioc = ioc;

            RegisterCommandHandlers();

            userRoleManager = this.ioc.Resolve<IUserRoleManager>();
        }

        public async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ChatMessage message)
        {
            if (messageHandler == null)
                messageHandler = ioc.Resolve<ITwitchChatMessageHandler>();

            if (messageHandler == null)
            {
                logger.LogInformation("HandleMessage: No message handler available.");
                return;
            }

            await messageHandler.HandleAsync(game, twitch, message);
        }

        public async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ChatCommand command)
        {
            var key = command.CommandText.ToLower();
            var session = game.GetSession(command.ChatMessage.Channel);
            var argString = !string.IsNullOrEmpty(command.ArgumentsAsString) ? " (args: " + command.ArgumentsAsString + ") " : "";

            if (session != null)
                logger.LogDebug("[" + session.Name + "] Command: " + key + argString + " from " + command.ChatMessage.Username);
            else
                logger.LogDebug("Twitch Command Received: " + key + argString + " from " + command.ChatMessage.Username + " in #" + command.ChatMessage.Channel);

            var uid = command.ChatMessage.UserId;
            await HandleAsync(game, twitch, new TwitchCommand(command, userRoleManager.IsAdministrator(uid), userRoleManager.IsModerator(uid)));
        }

        public async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, OnChannelPointsRewardRedeemedArgs reward)
        {
            //logger.LogInformation("Channel Point Rewards not implemented.");
            var cmd = "";
            var usedCommand = "";
            try
            {
                var redeemer = reward.RewardRedeemed.Redemption.User;
                var arguments = string.Empty;//reward.RewardRedeemed.Redemption.Reward.Prompt;
                var command = reward.RewardRedeemed.Redemption.Reward.Title;
                var cmdParts = command.ToLower().Split(' ');



                var session = game.GetSession(reward.ChannelId);

                // In case we use brackets to identify a command
                cmd = cmdParts.FirstOrDefault(x => x.Contains("["));
                usedCommand = cmd;

                if (!string.IsNullOrEmpty(cmd))
                    cmd = cmd.Replace("[", "").Replace("]", "").Trim();

                ITwitchCommandHandler processor = null;

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
                    processor = FindHandler(cmd.ToLower());
                    if (processor == null)
                    {
                        logger.LogError("Error redeeming reward: " + cmd + ", no handler found");
                        return;
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

                RavenBot.Core.Ravenfall.Models.Player player = null;
                if (processor.RequiresBroadcaster)
                {
                    player = session.GetBroadcaster();
                }
                else
                {
                    player = session.Get(new RewardRedeemUser(redeemer));
                    if (player == null)
                    {
                        logger.LogError("Error redeeming reward: " + usedCommand + ", redeemer does not exist. (" + redeemer.Id + ")");
                        return;
                    }
                }

                await processor.HandleAsync(game, twitch, new RewardRedeemCommand(player, reward.ChannelId, usedCommand, arguments));
            }
            catch (Exception exc)
            {
                logger.LogError("Error redeeming reward:  " + usedCommand + ", " + exc.ToString());
            }
        }

        private async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            var handler = FindHandler(cmd.Command);
            if (handler == null)
            {
                logger.LogInformation("HandleAsync::Unknown Command: " + cmd.Command + " - " + cmd.Arguments);
                return;
            }

            await handler.HandleAsync(game, twitch, cmd);
        }

        private ITwitchCommandHandler FindHandler(string command)
        {
            if (handlerLookup.TryGetValue(command, out var handlerType))
            {
                return ioc.Resolve(handlerType) as ITwitchCommandHandler;
            }

            return null;
        }

        private void RegisterCommandHandlers()
        {
            ioc.RegisterShared<ITwitchChatMessageHandler, TwitchChatMessageHandler>();

            var baseType = typeof(ITwitchCommandHandler);
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

        internal class RewardRedeemUser : ICommandSender
        {
            private readonly TwitchLib.PubSub.Models.Responses.Messages.User user;

            public RewardRedeemUser(TwitchLib.PubSub.Models.Responses.Messages.User user)
            {
                this.user = user;
            }

            public string UserId => user.Id;

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