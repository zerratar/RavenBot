using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RavenBot.Core;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Twitch;
using RavenBot.Forms;

namespace RavenBot
{
    class Program
    {
        internal static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            internal static extern Boolean AllocConsole();
        }

        [STAThread]
        static void Main(string[] args)
        {
            NativeMethods.AllocConsole();

            var ioc = new IoC();
            ioc.RegisterCustomShared<IoC>(() => ioc);
            ioc.RegisterShared<ILogger, ConsoleLogger>();
            ioc.RegisterShared<IKernel, Kernel>();

            ioc.RegisterShared<IUserRoleManager, UserRoleManager>();
            ioc.RegisterShared<IUserSettingsManager, UserSettingsManager>();
            ioc.RegisterShared<ITwitchUserStore, TwitchUserStore>();
            ioc.RegisterCustomShared<IAppSettings>(() => new AppSettingsProvider().Get());
            ioc.Register<IGameConnection, TcpGameConnection>();
            ioc.Register<IGameClient, TcpGameClient>();

            ioc.RegisterShared<IMessageBus, MessageBus>();
            ioc.RegisterShared<IChannelProvider, DefaultChannelProvider>();
            ioc.RegisterShared<IConnectionCredentialsProvider, DefaultConnectionCredentialsProvider>();
            ioc.RegisterShared<ICommandHandler, DefaultTextCommandHandler>();

            ioc.RegisterShared<IGameClient, TcpGameClient>();
            ioc.RegisterShared<IGameClient2, TcpGameClient>();
            ioc.RegisterShared<IGameClient3, TcpGameClient>();
            ioc.RegisterShared<IGameClient4, TcpGameClient>();

            ioc.RegisterShared<IPlayerProvider, PlayerProvider>();

            ioc.RegisterShared<IStringProvider, CachedStringProvider>();
            ioc.RegisterShared<IStringTemplateParser, StringTemplateParser>();
            ioc.RegisterShared<IStringTemplateProcessor, StringTemplateProcessor>();
            ioc.RegisterShared<IChatMessageFormatter, ChatMessageFormatter>();

            ioc.RegisterShared<ICommandProvider, CommandProvider>();

            ioc.RegisterShared<IRavenfallClient, UnityRavenfallClient>();
            ioc.RegisterShared<ICommandBindingProvider, CommandBindingProvider>();
            ioc.RegisterShared<ITwitchBot, TwitchBot>();


            var appSettings = ioc.Resolve<IAppSettings>();
            if (string.IsNullOrEmpty(appSettings.TwitchBotUsername) || string.IsNullOrEmpty(appSettings.TwitchBotAuthToken))
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new SettingsConfigurationForm());

                ioc.ReplaceSharedInstance(new AppSettingsProvider().Get());
            }


            using (var cmdListener = ioc.Resolve<ITwitchBot>())
            {
                cmdListener.Start();

                while (true)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Q)
                    {
                        break;
                    }
                }

                cmdListener.Stop();
            }
        }
    }
}
