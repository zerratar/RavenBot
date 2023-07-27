using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RavenBot.Core.Chat.Discord;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.GameServer;
using Shinobytes.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Helix;

namespace ROBot.Core.Chat.Discord
{
    public partial class DiscordCommandClient : IDiscordCommandClient
    {
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IAppSettings settings;
        private readonly IBotServer game;
        private readonly IMessageBus messageBus;
        private readonly IUserSettingsManager settingsManager;
        private readonly RavenBot.Core.IChatMessageFormatter messageFormatter;
        private readonly RavenBot.Core.IChatMessageTransformer messageTransformer;
        private readonly IDiscordCommandController commandHandler;

        private IMessageBusSubscription broadcastSubscription;
        private DiscordSocketClient discord;
        private bool disposed;

        private readonly ConcurrentDictionary<string, ulong> channelIdLookup = new();
        private readonly ConcurrentDictionary<string, DiscordCommand.DiscordChannel> channels = new();
        private readonly DiscordFormatMessager messager;

        private const long RavenfallGuildId = 694530158341783612;
        private const long PlayRavenfallCategoryId = 1114144307876724760;
        private const bool CreateTextChannelForStreamers = true; // set to true for now. but needs to be configurable.

        private List<SocketApplicationCommand> slashCommands = new List<SocketApplicationCommand>();
        private SocketGuild ravenfallGuild;
        private SemaphoreSlim creatingChannelMutex = new SemaphoreSlim(1);
        public DiscordCommandClient(
            ILogger logger,
            IKernel kernel,
            IAppSettings settings,
            IBotServer game,
            IMessageBus messageBus,
            IUserSettingsManager settingsManager,
            RavenBot.Core.IChatMessageFormatter messageFormatter,
            RavenBot.Core.IChatMessageTransformer messageTransformer,
            IDiscordCommandController commandHandler)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.settings = settings;
            this.game = game;
            this.messageBus = messageBus;
            this.settingsManager = settingsManager;
            this.messageFormatter = messageFormatter;
            this.messageTransformer = messageTransformer;
            this.commandHandler = commandHandler;

            commandHandler.ExportCommandDescriptors();

            messager = new DiscordFormatMessager();
            broadcastSubscription = messageBus.Subscribe<SessionGameMessageResponse>(MessageBus.Broadcast, Broadcast);

            discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                // How much logging do you want to see?
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.All,

                // If you or another service needs to do anything with messages
                // (eg. checking Reactions, checking the content of edited/deleted messages),
                // you must set the MessageCacheSize. You may adjust the number as needed.
                //MessageCacheSize = 50,
                MessageCacheSize = 100,
                // If your platform doesn't have native WebSockets,
                // add Discord.Net.Providers.WS4Net from NuGet,
                // add the `using` at the top, and uncomment this line:
                //WebSocketProvider = WS4NetProvider.Instance
            });
            discord.MessageReceived += HandleCommandAsync;
            discord.Log += Log;
            discord.Disconnected += Discord_Disconnected;
            discord.ButtonExecuted += Discord_ButtonExecuted;
            discord.Ready += Discord_Ready;
            discord.SlashCommandExecuted += Discord_SlashCommandExecuted;
        }


        private async Task Discord_Ready()
        {
            //var commands = commandHandler.RegisterSlashCommands(this, discord);
            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.               
                this.ravenfallGuild = discord.GetGuild(RavenfallGuildId); // ravenfall

                await ravenfallGuild.DeleteApplicationCommandsAsync();
                //slashCommands.AddRange(await guild.GetApplicationCommandsAsync());
                //if (slashCommands.Count != 0) return;
                //foreach (var command in commands)
                //{
                //    if (slashCommands.Any(x => x.Name == command.Name.Value))
                //        continue;
                //    this.slashCommands.Add(await guild.CreateApplicationCommandAsync(command));
                //    // With global commands we don't need the guild.
                //    //await discord.CreateGlobalApplicationCommandAsync(command);
                //    // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                //    // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
                //}

                await RebuildChannelListAsync();

            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }

        private async Task RebuildChannelListAsync()
        {
            var textChannels = ravenfallGuild.TextChannels.ToList();

            ConcurrentDictionary<string, ulong> newChannelIdLookup = new();
            ConcurrentDictionary<string, DiscordCommand.DiscordChannel> newChannels = new();

            foreach (var channel in textChannels)
            {
                if (channel.CategoryId != PlayRavenfallCategoryId)
                    continue;

                var key = channel.Name.ToLower();
                if (newChannels.ContainsKey(key))
                {
                    // we can't spam this, so we have to make sure we have a slight delay.
                    await Task.Delay(300);
                    await channel.DeleteAsync();
                    continue;
                }

                newChannels[key] = new DiscordCommand.DiscordChannel(channel);
                newChannelIdLookup[key] = channel.Id;
            }

            try
            {
                channels.Clear();
                channelIdLookup.Clear();

                foreach (var channel in newChannels)
                {
                    channels[channel.Key] = channel.Value;
                    channelIdLookup[channel.Key] = channel.Value.Id;
                }
            }
            catch (Exception exc)
            {
                logger.LogError("Error recreating channels list. " + exc);
            }
        }

        private async Task Discord_ButtonExecuted(SocketMessageComponent component)
        {
            await component.UpdateAsync(msg =>
            {
                msg.Components = null;
                msg.Content = "Nope";
            });
        }

        //private async Task Discord_Connected()
        //{            
        //    foreach (var value in channelIdLookup)
        //    {
        //        var channelId = value.Value;
        //        var channel = discord.GetChannel(channelId) as ISocketMessageChannel;
        //        if (channel != null)
        //        {
        //            channels[channel.Name] = new DiscordCommand.DiscordChannel(channel);
        //        }
        //    }
        //}

        private async Task Discord_Disconnected(Exception arg)
        {
            channels.Clear();
        }

        public async void Broadcast(SessionGameMessageResponse cmd)
        {
            if (cmd == null || cmd.Session?.Name == null)
            {
                return;
            }

            var channel = cmd.Session.Channel;
            if (channel == null)
            {
                cmd.Session.Channel = channel = await TryResolveChannelAsync(cmd.Session.Name);
            }

            if (channel != null)
            {
                var message = cmd.Message;
                if (message.Recipent.Platform == "system")
                {
                    // system message
                    SendMessage(channel, message.Format, message.Args);
                    return;
                }

                if (message.Recipent.Platform == "discord")
                {
                    if (!string.IsNullOrEmpty(message.CorrelationId) && ulong.TryParse(message.CorrelationId, out var replyId) && replyId != 0)
                    {
                        SendReply(channel, cmd.Message.Recipent, message.Format, message.Args, message.Category, message.Tags, replyId);
                        return;
                    }

                    // if we can't reply we should mention the recipent.
                    if (ulong.TryParse(message.Recipent.PlatformId, out var uid))
                    {
                        SendMessage(channel, message.Recipent, MentionUtils.MentionUser(uid) + " " + message.Format, message.Args, message.Category, message.Tags);
                    }
                    else
                    {
                        SendMessage(channel, message.Format, message.Args);
                    }
                    return;
                }

                // ignore any platform that is not discord.
                // SendMessage(channel, message.Format, message.Args);
            }
        }


        public async void SendReply(ICommand cmd, string message, params object[] args)
        {
            var channel = cmd.Channel;
            if (!string.IsNullOrEmpty(cmd.CorrelationId) && ulong.TryParse(cmd.CorrelationId, out var replyId) && replyId != 0)
            {
                SendReply(channel, cmd.Sender, message, args, string.Empty, new string[0], replyId);
                return;
            }

            SendMessage(channel, message, args);
        }

        public async void SendReply(
            ICommandChannel channel, ICommandSender recipent, string format, object[] args, string category, string[] tags, ulong replyId)
        {
            // unique to Discord, we can generate embeds and format our messages a bit more pretty
            // for special messages like !stats, we want to give a more appealing message. For these,
            // the formatter and transformation will be ignored as we will generate it ourselves.
            // but only if we can reply to a message
            var targetChannel = await GetChannelAsync(channel);
            if (targetChannel != null &&
                await messager.HandleCustomReplyAsync(targetChannel, new UserReference(recipent), new MessageReference(replyId), args, category, tags))
                return;

            SendReply(channel, format, args, replyId);
        }

        public async void SendReply(
            ICommandChannel channel, GameMessageRecipent recipent, string format, object[] args, string category, string[] tags, ulong replyId)
        {
            var targetChannel = await GetChannelAsync(channel);
            if (targetChannel != null &&
                await messager.HandleCustomReplyAsync(targetChannel, new UserReference(recipent), new MessageReference(replyId), args, category, tags))
                return;

            SendReply(channel, format, args, replyId);
        }

        public async void SendMessage(
            ICommandChannel channel, GameMessageRecipent recipent, string format, object[] args, string category, string[] tags)
        {
            var targetChannel = await GetChannelAsync(channel);
            if (targetChannel != null &&
                await messager.HandleCustomReplyAsync(targetChannel, new UserReference(recipent), null, args, category, tags))
                return;

            SendMessage(channel, format, args);
        }

        public async void SendReply(ICommandChannel channel, string format, object[] args, ulong replyId)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return;
            }

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
                return;

            await SendChatMessageAsync(channel, msg, new MessageReference(replyId));
        }

        public async void SendMessage(ICommandChannel channel, string format, object[] args)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                //logger.LogWarning($"[TWITCH] Broadcast Ignored - Empty Message (Channel: {channel} User: {user}");
                return;
            }

            var msg = messageFormatter.Format(format, args);
            if (string.IsNullOrEmpty(msg))
            {
                //logger.LogWarning($"[TWITCH] Broadcast Ignored - Message became empty after formatting (Channel: {channel} Format: '{format}' Args: '{string.Join(",", args)}')");
                return;
            }

            await SendChatMessageAsync(channel, msg, null);
        }

        public async void Start()
        {
            if (!kernel.Started) kernel.Start();
            try
            {
                // Login and connect.
                await discord.LoginAsync(TokenType.Bot,
                    settings.DiscordAuthToken
                );

                await discord.StartAsync();
            }
            catch (Exception exc)
            {
                logger.LogError("[DISCORD] Unable to start Discord Bot: " + exc);
            }
        }

        public void Stop()
        {
            discord.StopAsync();
        }

        public void Dispose()
        {
            if (disposed) return;
            try
            {
                discord.Dispose();
            }
            catch { }
            disposed = true;
        }

        public async Task SendChatMessageAsync(ICommandChannel channel, string message, MessageReference replyReference)
        {
            if (channel == null)
            {
                logger.LogWarning($"[DISCORD] Can't send message in target channel is null. (Message: {message})");
                return;
            }

            // Process the chat message a final time before sending it off.
            var c = await GetChannelAsync(channel);
            if (c != null)
            {
                var session = game.GetSession(channel);

                if (session != null && session.RavenfallUserId != Guid.Empty)
                {
                    var settings = settingsManager.Get(session.RavenfallUserId);
                    var transform = settings.ChatMessageTransformation;

                    if (transform == ChatMessageTransformation.TranslateAndPersonalize)
                    {
                        message = await messageTransformer.TranslateAndPersonalizeAsync(message, settings.ChatBotLanguage);
                    }
                    else if (transform == ChatMessageTransformation.Translate)
                    {
                        message = await messageTransformer.TranslateAsync(message, settings.ChatBotLanguage);
                    }
                    if (transform == ChatMessageTransformation.Personalize)
                    {
                        message = await messageTransformer.PersonalizeAsync(message);
                    }

                    logger.LogDebug($"[DISCORD] Sending Message (Channel: {channel} Message: {message} Language: {settings.ChatBotLanguage} Transformation: {transform})");
                }
                else
                {
                    logger.LogDebug($"[DISCORD] Sending Message (Channel: {channel} Message: {message})");
                }

                await c.SendMessageAsync(message, messageReference: replyReference);
            }
            else
            {
                logger.LogWarning($"[DISCORD] Can't send message in target channel as ID is unknown. (Channel: {channel} Message: {message})");
            }
        }

        private async Task<IMessageChannel> GetChannelAsync(ICommandChannel channel)
        {
            // ugly method, but it will try to create the channel if one does not exist.
            IMessageChannel msgChannel = null;

            if (channel is DiscordCommand.DiscordChannel discordChannel && discordChannel.Channel != null)
            {
                return discordChannel.Channel;
            }

            if (channel != null)
            {
                var c0 = await TryResolveChannelAsync(channel.Name);
                if (c0 != null) msgChannel = c0.Channel;
            }

            return msgChannel;
        }

        private async Task<DiscordCommand.DiscordChannel> TryResolveChannelAsync(string name)
        {
            try
            {
                await creatingChannelMutex.WaitAsync();

                var key = name.ToLower();
                if (channels.TryGetValue(key, out var channel))
                {
                    return channel;
                }

                // we will create a channel if one does not exist.
                if (!CreateTextChannelForStreamers)
                {
                    return null;
                }

                // before we create one, make sure there isnt one already.

                IMessageChannel msgChannel = null;

                if (channelIdLookup.TryGetValue(key, out var channelId))
                {
                    msgChannel = ravenfallGuild.GetTextChannel(channelId);
                }

                // cant find the channel after all, rebuild channel list.
                if (msgChannel == null)
                {
                    await RebuildChannelListAsync();

                    if (channels.TryGetValue(key, out var c))
                    {
                        return c;
                    }
                }

                // if it is still null, create it.
                if (msgChannel == null)
                {
                    msgChannel = await ravenfallGuild.CreateTextChannelAsync(name, props => props.CategoryId = PlayRavenfallCategoryId);
                }

                channelIdLookup[key] = msgChannel.Id;
                return channels[key] = new DiscordCommand.DiscordChannel(msgChannel);
            }
            catch (Exception exc)
            {
                logger.LogError("Failed to create Ravenfall Bot Channel for " + name + ". Exception: " + exc);
            }
            finally
            {
                creatingChannelMutex.Release();
            }
            return null;
        }

        private async Task Discord_SlashCommandExecuted(SocketSlashCommand msg)
        {
            var key = msg.Channel.Name.ToLower();

            if (!channels.ContainsKey(key))
            {
                channelIdLookup[key] = msg.Channel.Id;
                channels[key] = new DiscordCommand.DiscordChannel(msg.Channel);
            }

            Log(new LogMessage(LogSeverity.Info, "", msg.User.Username + " used /" + msg.CommandName));

            await commandHandler.HandleAsync(game, this, msg);//msg.Data);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var key = arg.Channel.Name.ToLower();

            if (!channels.ContainsKey(key))
            {
                channelIdLookup[key] = arg.Channel.Id;
                channels[key] = new DiscordCommand.DiscordChannel(arg.Channel);
            }

            // Bail out if it's a System Message.
            var msg = arg as SocketUserMessage;
            if (msg == null) return;
            if (msg.Author.Id == discord.CurrentUser.Id || msg.Author.IsBot) return;


            // Create a number to track where the prefix ends and the command begins
            int pos = 0;

            Log(new LogMessage(LogSeverity.Info, "", msg.CleanContent));

            if (msg.HasCharPrefix('!', ref pos))
            {
                await commandHandler.HandleAsync(game, this, arg);
            }
        }

        private Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            logger.LogInformation($"[DISCORD] [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();

            return Task.CompletedTask;
        }

        public async void SessionEnded(IGameSession session)
        {
            // delete the channel?
            await TrySetChannelTopicAsync(session.Name.ToLower(), "Game Status: Offline");
        }

        public async void EnsureSessionChannel(IGameSession session)
        {
            // create the channel?
            var channel = await TryResolveChannelAsync(session.Name);
            if (session.Channel == null && channel != null)
            {
                session.Channel = channel;
            }

            await TrySetChannelTopicAsync(session.Name.ToLower(), "Game Status: Online! - Use !join to play");
        }

        private async Task TrySetChannelTopicAsync(string key, string topic)
        {
            try
            {
                if (channels.TryGetValue(key, out var c) && c.Channel is SocketTextChannel channel)
                {
                    await channel.ModifyAsync(prop => prop.Topic = topic);
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
