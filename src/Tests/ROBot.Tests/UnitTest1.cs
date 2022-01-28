using Microsoft.VisualStudio.TestTools.UnitTesting;
using RavenBot.Core.Ravenfall.Commands;
using ROBot.Core.Twitch;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task InvokeAllChatCommands_NoArgs()
        {
            var logger = new ConsoleLogger();

            var ioc = new Shinobytes.Ravenfall.RavenNet.Core.IoC();
            ioc.RegisterShared<IUserRoleManager, UserRoleManager>();
            ioc.RegisterShared<IMessageBus, MessageBus>();

            var server = new MockBotServer();
            var client = new MockTwitchCommandClient();
            var commandController = new TwitchCommandController(logger, ioc);

            foreach (var cmds in commandController.RegisteredCommandHandlers)
            {
                var cmd = GetChatCommand(cmds.Name.ToLower());
                if (!await commandController.HandleAsync(server, client, cmd))
                {
                    Assert.Fail(cmd.CommandText + " " + cmd.ArgumentsAsString + " - Failed.");
                }
            }
        }

        [TestMethod]
        public async Task InvokeAllChatCommands_EmptyArgs()
        {
            var logger = new ConsoleLogger();

            var ioc = new Shinobytes.Ravenfall.RavenNet.Core.IoC();
            ioc.RegisterShared<IUserRoleManager, UserRoleManager>();
            ioc.RegisterShared<IMessageBus, MessageBus>();

            var server = new MockBotServer();
            var client = new MockTwitchCommandClient();
            var commandController = new TwitchCommandController(logger, ioc);

            foreach (var cmds in commandController.RegisteredCommandHandlers)
            {
                var cmd = GetChatCommand(cmds.Name.ToLower(), "    ");
                if (!await commandController.HandleAsync(server, client, cmd))
                {
                    Assert.Fail(cmd.CommandText + " - Failed.");
                }
            }
        }


        //[TestMethod]
        //public async Task InvokeAllChatCommands_CommandIsNull()
        //{
        //    var logger = new ConsoleLogger();

        //    var ioc = new Shinobytes.Ravenfall.RavenNet.Core.IoC();
        //    ioc.RegisterShared<IUserRoleManager, UserRoleManager>();
        //    ioc.RegisterShared<IMessageBus, MessageBus>();

        //    var server = new MockBotServer();
        //    var client = new MockTwitchCommandClient();
        //    var commandController = new TwitchCommandController(logger, ioc);

        //    foreach (var cmds in commandController.RegisteredCommandHandlers)
        //    {
        //        var handler = commandController.GetHandler(cmds.Name.ToLower());
        //        RavenBot.Core.Handlers.ICommand cmd = null;
        //        try { await handler.HandleAsync(server, client, cmd); }
        //        catch (Exception exc)
        //        {
        //            Assert.Fail(cmds.Name + " - Failed. " + exc);
        //        }
        //    }
        //}

        [TestMethod]
        public async Task InvokeAllChatCommands_ArgIsNull()
        {
            var logger = new ConsoleLogger();

            var ioc = new Shinobytes.Ravenfall.RavenNet.Core.IoC();
            ioc.RegisterShared<IUserRoleManager, UserRoleManager>();
            ioc.RegisterShared<IMessageBus, MessageBus>();

            var server = new MockBotServer();
            var client = new MockTwitchCommandClient();
            var commandController = new TwitchCommandController(logger, ioc);

            foreach (var cmds in commandController.RegisteredCommandHandlers)
            {
                var cmd = GetChatCommand(cmds.Name.ToLower(), null);
                if (!await commandController.HandleAsync(server, client, cmd))
                {
                    Assert.Fail(cmd.CommandText + " - Failed.");
                }
            }
        }


        [TestMethod]
        public async Task InvokeAllChatCommands_Jibberish()
        {
            var logger = new ConsoleLogger();

            var ioc = new Shinobytes.Ravenfall.RavenNet.Core.IoC();
            ioc.RegisterShared<IUserRoleManager, UserRoleManager>();
            ioc.RegisterShared<IMessageBus, MessageBus>();

            var server = new MockBotServer();
            var client = new MockTwitchCommandClient();
            var commandController = new TwitchCommandController(logger, ioc);

            foreach (var cmds in commandController.RegisteredCommandHandlers)
            {
                var cmd = GetChatCommand(cmds.Name.ToLower(), "wehawh322h32q34q2");
                if (!await commandController.HandleAsync(server, client, cmd))
                {
                    Assert.Fail(cmd.CommandText + " " + cmd.ArgumentsAsString + " - Failed.");
                }
            }
        }


        [TestMethod]
        public async Task InvokeAllChatCommands_Jibberish_x2()
        {
            var logger = new ConsoleLogger();

            var ioc = new Shinobytes.Ravenfall.RavenNet.Core.IoC();
            ioc.RegisterShared<IUserRoleManager, UserRoleManager>();
            ioc.RegisterShared<IMessageBus, MessageBus>();

            var server = new MockBotServer();
            var client = new MockTwitchCommandClient();
            var commandController = new TwitchCommandController(logger, ioc);

            foreach (var cmds in commandController.RegisteredCommandHandlers)
            {
                var cmd = GetChatCommand(cmds.Name.ToLower(), "wehawh32 12tsq1");
                if (!await commandController.HandleAsync(server, client, cmd))
                {
                    Assert.Fail(cmd.CommandText + " " + cmd.ArgumentsAsString + " - Failed.");
                }
            }
        }

        [TestMethod]
        public async Task InvokeAllChatCommands_UnicodeCharacter()
        {
            var logger = new ConsoleLogger();

            var ioc = new Shinobytes.Ravenfall.RavenNet.Core.IoC();
            ioc.RegisterShared<IUserRoleManager, UserRoleManager>();
            ioc.RegisterShared<IMessageBus, MessageBus>();

            var server = new MockBotServer();
            var client = new MockTwitchCommandClient();
            var commandController = new TwitchCommandController(logger, ioc);

            foreach (var cmds in commandController.RegisteredCommandHandlers)
            {
                var cmd = GetChatCommand(cmds.Name.ToLower(), "\u4215");
                if (!await commandController.HandleAsync(server, client, cmd))
                {
                    Assert.Fail(cmd.CommandText + " " + cmd.ArgumentsAsString + " - Failed.");
                }
            }
        }

        private TwitchLib.Client.Models.ChatCommand GetChatCommand(string command, string args = "")
        {
            var sender = "RavenfallOfficial";
            var channel = "zerratar";

            return new TwitchLib.Client.Models.ChatCommand(
                    new TwitchLib.Client.Models.ChatMessage(sender, "123456", sender, channel, "ffffff", System.Drawing.Color.White, null,
                    "!" + command, TwitchLib.Client.Enums.UserType.Moderator, channel, channel, true, 1, "12345",
                    false, true, false, false, true, true, false, TwitchLib.Client.Enums.Noisy.False, "", "",
                    new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>(),
                    new TwitchLib.Client.Models.CheerBadge(100), 0, 0),
                    command, args, args?.Split(' ')?.ToList() ?? new System.Collections.Generic.List<string>(), '!'
                );
        }
    }
}