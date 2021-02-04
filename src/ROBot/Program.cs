using System;
using ROBot.Core;
using ROBot.Core.Twitch;
using Shinobytes.Ravenfall.RavenNet.Core;
using Microsoft.Extensions.Logging;
using ROBot.Ravenfall;
using ROBot.Core.GameServer;
using IAppSettings = ROBot.Core.IAppSettings;
using RavenBot.Core.Twitch;

namespace ROBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var ioc = new IoC();

            ioc.RegisterCustomShared<IoC>(() => ioc);
            ioc.RegisterCustomShared<IAppSettings>(() => new AppSettingsProvider().Get());
            ioc.RegisterCustomShared<IBotServerSettings>(() => new BotServerSettings
            {
                ServerIp = "127.0.0.1",
                ServerPort = 4041
            });

            ioc.RegisterShared<ILogger, ConsoleLogger>();
            ioc.RegisterShared<IKernel, Kernel>();
            ioc.RegisterShared<IStreamBotApplication, StreamBotApp>();

            ioc.RegisterShared<IMessageBus, MessageBus>();

            ioc.RegisterShared<RavenBot.Core.IStringProvider, RavenBot.Core.StringProvider>();
            ioc.RegisterShared<RavenBot.Core.IStringTemplateParser, RavenBot.Core.StringTemplateParser>();
            ioc.RegisterShared<RavenBot.Core.IStringTemplateProcessor, RavenBot.Core.StringTemplateProcessor>();
            ioc.RegisterShared<ITwitchMessageFormatter, TwitchMessageFormatter>();

            // Ravenfall stuff
            ioc.RegisterShared<IBotServer, BotServer>();
            ioc.RegisterShared<IGameSessionManager, GameSessionManager>();
            ioc.RegisterShared<IRavenfallConnectionProvider, RavenfallConnectionProvider>();
            ioc.RegisterShared<RavenBot.Core.Ravenfall.Commands.IPlayerProvider, RavenBot.Core.Ravenfall.Commands.PlayerProvider>();

            // Twitch stuff
            ioc.RegisterShared<ITwitchCredentialsProvider, TwitchCredentialsProvider>();
            ioc.RegisterShared<ITwitchCommandController, TwitchCommandController>();
            ioc.RegisterShared<ITwitchCommandClient, TwitchCommandClient>();

            var app = ioc.Resolve<IStreamBotApplication>();
            {
                app.Run();
                while (true)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                    {
                        break;
                    }
                }
                app.Shutdown();
            }
        }
    }
}
