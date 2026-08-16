using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RavenBot.Core;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Templating;
using Shinobytes.Core;
using ConsoleLogger = RavenBot.Core.ConsoleLogger;
using IAppSettings = RavenBot.Core.IAppSettings;

namespace RavenBot
{
    class Program
    {
        async static Task Main(string[] args)
        {
            if (args != null && args.Any(x => x != null && !x.ToLower().Contains("ravenbot") && !x.ToLower().Contains("/") && x.ToLower().Contains("install")))
            {
                InstallAsService();
                return;
            }

            var ioc = new IoC();
            ioc.RegisterCustomShared<IoC>(() => ioc);
            ioc.RegisterShared<ILogger, ConsoleLogger>();
            ioc.RegisterShared<IKernel, Kernel>();

#if DEBUG
            ioc.RegisterCustomShared<IUserSettingsManager>(() => new UserSettingsManager(@"C:\Ravenfall\Data\user-settings"));
#else
            ioc.RegisterCustomShared<IUserSettingsManager>(() => new UserSettingsManager("./user-settings/"));
#endif
            ioc.RegisterShared<ITwitchUserStore, TwitchUserStore>();
            ioc.RegisterCustomShared<IAppSettings>(() => new AppSettingsProvider().Get());

            ioc.Register<IGameConnection, TcpGameConnection>();
            //ioc.Register<IGameClient, TcpGameClient>();

            //ioc.RegisterShared<IMessageBus, MessageBus>();
            ioc.RegisterCustomShared<IMessageBus>(() => MessageBus.Instance);

            ioc.RegisterShared<IChannelProvider, DefaultChannelProvider>();
            ioc.RegisterShared<IConnectionCredentialsProvider, DefaultConnectionCredentialsProvider>();
            ioc.RegisterShared<ICommandHandler, DefaultTextCommandHandler>();

            ioc.RegisterShared<IGameClient, TcpGameClient>();

            ioc.RegisterShared<IUserProvider, UserProvider>();

            ioc.RegisterShared<IStringProvider, CachedStringProvider>();
            ioc.RegisterShared<IStringTemplateParser, StringTemplateParser>();
            ioc.RegisterShared<IStringTemplateProcessor, StringTemplateProcessor>();
            ioc.RegisterShared<IChatMessageFormatter, ChatMessageFormatter>();

            ioc.RegisterShared<ICommandProvider, CommandProvider>();

            ioc.RegisterShared<IRavenfallClient, UnityRavenfallClient>();
            ioc.RegisterShared<ICommandBindingProvider, CommandBindingProvider>();
            ioc.RegisterShared<ITwitchBot, TwitchBot>();

            var appSettings = ioc.Resolve<IAppSettings>();

            if (string.IsNullOrEmpty(appSettings.TwitchBotUsername))
            {
                Console.WriteLine("Missing Twitch Bot Username!");
                Console.Write("Please enter the username of your Twitch Bot: ");
                appSettings.TwitchBotUsername = Console.ReadLine();
                appSettings.Save();
            }

            if (string.IsNullOrEmpty(appSettings.TwitchBotAuthToken))
            {
                Console.WriteLine("Missing Twitch Bot Auth Token!");
                Console.Write("Please enter the username of your Twitch Bot: ");
                appSettings.TwitchBotUsername = Console.ReadLine();
                appSettings.Save();
            }
            //{
            //    Application.SetHighDpiMode(HighDpiMode.SystemAware);
            //    Application.EnableVisualStyles();
            //    Application.SetCompatibleTextRenderingDefault(false);
            //    Application.Run(new SettingsConfigurationForm());
            //    ioc.ReplaceSharedInstance(new AppSettingsProvider().Get());
            //}

            Console.WriteLine("Press Q to exit at any time.");
            Console.WriteLine("Press I to install as service. (This will restart the bot)");
            var installAsService = false;
            using (var cmdListener = ioc.Resolve<ITwitchBot>())
            {
                await cmdListener.StartAsync();

                while (true)
                {
                    var k = Console.ReadKey();
                    if (k.Key == ConsoleKey.Q)
                    {
                        break;
                    }

                    if (k.Key == ConsoleKey.I)
                    {
                        cmdListener.Stop();
                        installAsService = true;
                        // install as service, LINUX only.
                        break;
                    }
                }

                if (!installAsService)
                    cmdListener.Stop();
            }

            if (installAsService)
            {
                InstallAsService();
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
            }
        }


        private static void InstallAsService()
        {
            Console.WriteLine("Installing RavenBot as ravenbot.service...");

            // Get the current executable path
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // Define the service file content
            string serviceContent = $@"
[Unit]
Description=RavenBot Service

[Service]
ExecStart=""{exePath}""
WorkingDirectory=""{Path.GetDirectoryName(exePath)}""
Restart=always

[Install]
WantedBy=multi-user.target";

            // Path to the .service file
            string serviceFilePath = "/etc/systemd/system/ravenbot.service";

            // Write the service file
            File.WriteAllText(serviceFilePath, serviceContent);

            // Set permissions on the service file
            ExecuteCommand("chmod 644 /etc/systemd/system/ravenbot.service");

            // Reload systemd daemon
            ExecuteCommand("systemctl daemon-reload");

            // Start the service
            ExecuteCommand("systemctl start ravenbot.service");

            // Enable the service to start on boot
            ExecuteCommand("systemctl enable ravenbot.service");

            Console.WriteLine("RavenBot service installed successfully.");
        }

        private static void ExecuteCommand(string command)
        {
            ProcessStartInfo procStartInfo = new ProcessStartInfo("bash", "-c " + command)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process proc = new Process() { StartInfo = procStartInfo };
            proc.Start();
            proc.WaitForExit();
        }
    }
}
