using System;
using RavenBot.Core;
using RavenBot.Core.Net;
using RavenBot.Core.Twitch;

namespace RavenBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var ioc = new IoC();
            ioc.RegisterShared<ILogger, ConsoleLogger>();
            ioc.RegisterShared<ITwitchUserStore, TwitchUserStore>();

            ioc.RegisterCustomShared<IAppSettings>(() => new AppSettingsProvider().Get());

            ioc.Register<IGameConnection, TcpGameConnection>();
            ioc.Register<IGameClient, TcpGameClient>();

            ioc.RegisterShared<IMessageBus, MessageBus>();
            ioc.RegisterShared<IChannelProvider, DefaultChannelProvider>();
            ioc.RegisterShared<IConnectionCredentialsProvider, DefaultConnectionCredentialsProvider>();

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    return;
                }
            }
        }
    }
}
