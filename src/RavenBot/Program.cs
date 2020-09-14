using System;
using RavenBot.Core;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Twitch;

namespace RavenBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var ioc = new IoC();
            ioc.RegisterCustomShared<IoC>(() => ioc);
            ioc.RegisterShared<ILogger, ConsoleLogger>();
            ioc.RegisterShared<IKernel, Kernel>();

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

            ioc.RegisterShared<ICommandListener, TwitchCommandListener>();
            ioc.RegisterShared<IRavenfallClient, UnityRavenfallClient>();
            ioc.RegisterShared<ICommandBindingProvider, CommandBindingProvider>();

            using (var cmdListener = ioc.Resolve<ICommandListener>())
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
