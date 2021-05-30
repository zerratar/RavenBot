using RavenBot.Core;
using Shinobytes.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteManagement
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            IoC ioc = RegisterServices();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(ioc.Resolve<MainForm>());
        }

        private static IoC RegisterServices()
        {
            var ioc = new IoC();
            ioc.RegisterCustomShared<IoC>(() => ioc);
            ioc.RegisterShared<MainForm, MainForm>();
            ioc.RegisterShared<ILogger, ConsoleLogger>();

            // Setting up client requirements
            ioc.Register<IClient, Client>();
            ioc.RegisterShared<IServerPacketSerializer, BinaryServerPacketSerializer>();

            // Setting up the server
            ioc.RegisterCustomShared<ServerSettings>(() => new ServerSettings("ravenbot.ravenfall.stream", 6767));
            ioc.RegisterShared<IServer, TcpServer>();
            ioc.RegisterShared<IServerConnectionManager, ServerConnectionManager>();
            ioc.RegisterShared<IServerClientProvider, ServerClientProvider>();

            return ioc;
        }
    }
}
