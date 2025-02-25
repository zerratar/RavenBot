using System;
using ROBot.Core;
using Microsoft.Extensions.Logging;
using ROBot.Core.GameServer;
using IAppSettings = ROBot.Core.IAppSettings;
using Shinobytes.Network;
using System.Net;
using ROBot.Core.Stats;
using ROBot.Core.OpenAI;
using ROBot.Core.Chat.Twitch.PubSub;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.Chat.Discord;
using RavenBot.Core.Templating;
using Shinobytes.Core;
using ROBot.API;
using RavenBot.Core.Ravenfall;
using System.Threading.Tasks;
//using RavenBot.Core;

namespace ROBot
{
    class Program
    {
        private static IoC ioc;
        private static bool isExiting;
        async static Task Main(string[] args)
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

            //ioc.RegisterShared<IMessageBus, MessageBus>();
            ioc.RegisterCustomShared<IMessageBus>(() => MessageBus.Instance);

            //#if DEBUG && 
            //            ioc.RegisterCustomShared<IUserSettingsManager>(() => new UserSettingsManager(@"C:\Ravenfall\user-settings"));
            //#else

#if Windows
            ioc.RegisterCustomShared<IUserSettingsManager>(() => new UserSettingsManager("G:\\Ravenfall\\Data\\user-settings"));
#else
            ioc.RegisterCustomShared<IUserSettingsManager>(() => new UserSettingsManager());
#endif

            //#endif

            ioc.RegisterShared<IStringProvider, StringProvider>();
            ioc.RegisterShared<IStringTemplateParser, StringTemplateParser>();
            ioc.RegisterShared<IStringTemplateProcessor, StringTemplateProcessor>();
            ioc.RegisterShared<RavenBot.Core.IChatMessageFormatter, RavenBot.Core.ChatMessageFormatter>();
            ioc.RegisterShared<RavenBot.Core.IChatMessageTransformer, ChatGPTMessageTransformer>();


            // Ravenfall stuff
            ioc.RegisterShared<IBotServer, BotServer>();
            ioc.RegisterShared<IGameSessionManager, GameSessionManager>();
            ioc.RegisterShared<IRavenfallConnectionProvider, RavenfallConnectionProvider>();
            ioc.RegisterShared<IUserProvider, UserProvider>();

            // Twitch stuff
            ioc.RegisterShared<ITwitchCredentialsProvider, TwitchCredentialsProvider>();
            ioc.RegisterShared<ITwitchCommandController, TwitchCommandController>();
            ioc.RegisterShared<ITwitchCommandClient, TwitchCommandClient>();

            ioc.RegisterShared<ITwitchPubSubManager, TwitchPubSubManager>();

            ioc.RegisterShared<IDiscordCommandController, DiscordCommandController>();
            ioc.RegisterShared<IDiscordCommandClient, DiscordCommandClient>();

            //stats ... what the hell am I doing. Gawd.
            object stats = new Stats();
            ioc.RegisterCustomShared<IBotStats>(() => stats);
            ioc.RegisterCustomShared<ITwitchStats>(() => stats);


            // Log extraction
            // Setting up the server
            ioc.RegisterCustomShared<ServerSettings>(() => new ServerSettings(ServerHost, LogServerPort));
            ioc.RegisterShared<IServer, TcpServer>();

            ioc.RegisterShared<IServerClientProvider, ServerClientProvider>();
            ioc.RegisterShared<IServerPacketHandlerProvider, ServerPacketHandlerProvider>();
            ioc.RegisterShared<IServerPacketSerializer, BinaryServerPacketSerializer>();
            ioc.RegisterShared<IAdminAPIEndpointServer, AdminTcpAPIEndpointServer>();

            var mb = ioc.Resolve<IMessageBus>();

            ioc.RegisterCustomShared<ILogger>(() => new PersistedConsoleLogger(mb));

            ioc.RegisterShared<ILogger, PersistedConsoleLogger>();

            ioc.Resolve<IAdminAPIEndpointServer>();

            var app = ioc.Resolve<IStreamBotApplication>();
            {
                await app.RunAsync();
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
