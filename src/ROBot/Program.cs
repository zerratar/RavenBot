using System;
using ROBot.Core;
using ROBot.Core.Twitch;
using Shinobytes.Ravenfall.RavenNet.Core;
using Microsoft.Extensions.Logging;
using ROBot.Ravenfall;
using ROBot.Core.GameServer;
using IAppSettings = ROBot.Core.IAppSettings;
using RavenBot.Core.Twitch;
using Shinobytes.Network;

namespace ROBot
{
    class Program
    {
        private static bool isExiting;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            var ioc = new IoC();

            ioc.RegisterCustomShared<IoC>(() => ioc);
            ioc.RegisterCustomShared<IAppSettings>(() => new AppSettingsProvider().Get());
            ioc.RegisterCustomShared<IBotServerSettings>(() => new BotServerSettings
            {
                ServerIp = "0.0.0.0",
                ServerPort = 4041
            });


            //ioc.RegisterShared<ILogger, ConsoleLogger>();
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



            // Log extraction
            // Setting up the server
            ioc.RegisterCustomShared<ServerSettings>(() => new ServerSettings("0.0.0.0", 6767));
            ioc.RegisterShared<IServer, TcpServer>();
            ioc.RegisterShared<IServerConnectionManager, ServerConnectionManager>();
            ioc.RegisterShared<IServerClientProvider, ServerClientProvider>();
            ioc.RegisterShared<IServerPacketHandlerProvider, ServerPacketHandlerProvider>();
            ioc.RegisterShared<IServerPacketSerializer, BinaryServerPacketSerializer>();
            ioc.RegisterShared<ILogger, ConsoleLogServer>();

            var app = ioc.Resolve<IStreamBotApplication>();
            {
                app.Run();
                while (!isExiting)
                {
                    //if (Console.ReadKey().Key == ConsoleKey.Q)
                    //{
                    //    break;
                    //}
                    System.Threading.Thread.Sleep(10);
                }
                app.Shutdown();
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            isExiting = true;
        }
    }
}
