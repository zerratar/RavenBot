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
using RavenBot.Core.Ravenfall.Commands;
using System.Net;
using ROBot.Core.Stats;

namespace ROBot
{
    class Program
    {
        private static IoC ioc;
        private static bool isExiting;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            const int LogServerPort = 6767;
            const int BotServerPort = 4041;
            const string ServerHost = "0.0.0.0";

            ioc = new IoC();
            ioc.RegisterCustomShared<IoC>(() => ioc);
            ioc.RegisterCustomShared<IAppSettings>(() => new AppSettingsProvider().Get());
            ioc.RegisterCustomShared<IBotServerSettings>(() => new BotServerSettings
            {
                ServerIp = ServerHost,
                ServerPort = BotServerPort
            });

            //ioc.RegisterShared<ILogger, ConsoleLogger>();
            ioc.RegisterShared<IKernel, Kernel>();
            ioc.RegisterShared<IStreamBotApplication, StreamBotApp>();

            ioc.RegisterShared<IMessageBus, MessageBus>();

            ioc.RegisterShared<IUserRoleManager, UserRoleManager>();
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

            ioc.RegisterShared<ITwitchPubSubManager, TwitchPubSubManager>();
            ioc.RegisterShared<ITwitchPubSubTokenRepository, TwitchPubSubTokenRepository>();

            //stats ... what the hell am I doing. Gawd.
            object stats = new Stats();
            ioc.RegisterCustomShared<IBotStats>(() => stats);
            ioc.RegisterCustomShared<ITwitchStats>(() => stats);


            // Log extraction
            // Setting up the server
            ioc.RegisterCustomShared<ServerSettings>(() => new ServerSettings(ServerHost, LogServerPort));
            ioc.RegisterShared<IServer, TcpServer>();

            ioc.RegisterShared<IServerConnectionManager, ServerConnectionManager>();
            ioc.RegisterShared<IServerClientProvider, ServerClientProvider>();
            ioc.RegisterShared<IServerPacketHandlerProvider, ServerPacketHandlerProvider>();
            ioc.RegisterShared<IServerPacketSerializer, BinaryServerPacketSerializer>();
            ioc.RegisterShared<IAdminAPIEndpointServer, AdminTcpAPIEndpointServer>();

            ioc.RegisterShared<ILogger, PersistedConsoleLogger>();
            
            ioc.Resolve<IAdminAPIEndpointServer>();
            
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

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (ioc != null)
            {
                try
                {
                    var logger = ioc.Resolve<ILogger>() as PersistedConsoleLogger;
                    if (logger != null)
                    {
                        logger.LogError("[SPECIAL] " + e.ToString() + " | " + e.ExceptionObject.ToString());
                        logger.TrySaveLogToDisk();
                    }
                    else
                    {
                        System.Console.WriteLine("[UNSAVED] " + e.ToString() + " | " + e.ExceptionObject.ToString());
                    }
                }
                catch (Exception exc)
                {
                    System.Console.WriteLine("[CAUGHT] " + exc.ToString());
                }
            }
            else
            {
                System.Console.WriteLine("[CAUGHT] with null ioc Error: " + e.ToString() + " | " + e.ExceptionObject.ToString());
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {

            try
            {
                var logger = ioc.Resolve<ILogger>() as PersistedConsoleLogger;
                if (logger != null)
                {
                    logger.TrySaveLogToDisk();
                }
            }
            catch (Exception exc)
            {
                System.Console.WriteLine("[CAUGHT] Exception trying to save logger Error: " + exc.ToString());
                System.Console.WriteLine("[CAUGHT] extended info on EventArgs " + e.ToString());
            }

            isExiting = true;
        }
    }
}
