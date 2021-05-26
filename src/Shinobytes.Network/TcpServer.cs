using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shinobytes.Network
{
    public class TcpServer : IServer
    {
        public event EventHandler<ConnectionEventArgs> ClientConnected;
        public event EventHandler<ConnectionEventArgs> ClientDisconnected;

        private readonly ServerSettings settings;
        private readonly IServerClientProvider clientProvider;
        private readonly IServerConnectionManager connectionManager;
        private readonly TcpListener tcpServer;
        private bool disposed;
        private CancellationToken cancellationToken;

        public TcpServer(
            ServerSettings settings,
            IServerClientProvider clientProvider,
            IServerConnectionManager connectionManager)
        {
            this.settings = settings;
            this.clientProvider = clientProvider;
            this.connectionManager = connectionManager;
            if (!IPAddress.TryParse(this.settings.host, out var ip))
            {
                ip = IPAddress.Any;
            }

            this.tcpServer = new TcpListener(new IPEndPoint(ip, this.settings.port));
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            this.Stop();
            disposed = true;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(TcpServer));
            }

            if (this.tcpServer.Server.IsBound)
            {
                return Task.CompletedTask;
            }

            this.cancellationToken = cancellationToken;
            this.tcpServer.Start();
            this.tcpServer.BeginAcceptTcpClient(OnClientConnected, this.tcpServer);
            return Task.CompletedTask;
        }

        private void OnClientConnected(IAsyncResult ar)
        {
            if (this.cancellationToken.IsCancellationRequested)
            {
                this.Dispose();
                return;
            }

            try
            {
                var tcpClient = this.tcpServer.EndAcceptTcpClient(ar);
                var client = clientProvider.Get(tcpClient);

                client.Disconnected += Client_Disconnected;
                connectionManager.Add(client);

                if (ClientConnected != null)
                {
                    ClientConnected.Invoke(this, new ConnectionEventArgs(client));
                }
            }
            catch { }

            try
            {
                this.tcpServer.BeginAcceptTcpClient(OnClientConnected, this.tcpServer);
            }
            catch { }
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            var client = sender as INetworkClient;
            client.Disconnected -= Client_Disconnected;
            connectionManager.Remove(client);
            if (ClientDisconnected != null)
            {
                ClientDisconnected.Invoke(this, new ConnectionEventArgs(client));
            }
        }

        public void Stop()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(TcpServer));
            }

            if (this.tcpServer.Server.IsBound)
            {
                this.tcpServer.Stop();
            }
        }
    }
}
