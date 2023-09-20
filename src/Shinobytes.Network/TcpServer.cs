using Microsoft.Extensions.Logging;
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

        private readonly ILogger logger;
        private readonly ServerSettings settings;
        private readonly IServerClientProvider clientProvider;
        private TcpListener tcpServer;
        private bool disposed;
        private CancellationToken cancellationToken;

        public TcpServer(
            ILogger logger,
            ServerSettings settings,
            IServerClientProvider clientProvider)
        {
            this.logger = logger;
            this.settings = settings;
            this.clientProvider = clientProvider;
            if (!IPAddress.TryParse(this.settings.host, out var ip))
            {
                ip = IPAddress.Any;
            }

            this.tcpServer = new TcpListener(new IPEndPoint(ip, this.settings.port));
        }

        public Exception LastException { get; private set; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            this.Stop();
            disposed = true;
        }

        public Task<bool> StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(TcpServer));
                }

                if (this.tcpServer.Server.IsBound)
                {
                    return Task.FromResult(false);
                }

                this.cancellationToken = cancellationToken;
                this.tcpServer.Start();
                this.tcpServer.BeginAcceptTcpClient(OnClientConnected, this.tcpServer);
                return Task.FromResult(true);
            }
            catch (Exception exc)
            {
                LastException = exc;
                logger.LogError(exc.ToString());
                return Task.FromResult(false);
            }
        }

        private void OnClientConnected(IAsyncResult ar)
        {
            if (this.cancellationToken.IsCancellationRequested)
            {
                this.Dispose();
                return;
            }

            TcpClient tcpClient = null;
            IServerClient client = null;

            try
            {
                tcpClient = this.tcpServer.EndAcceptTcpClient(ar);
                if (tcpClient == null)
                {
                    AcceptConnections();
                    return;
                }

                client = clientProvider.Get(tcpClient);

                client.Disconnected += Client_Disconnected;

                if (ClientConnected != null)
                {
                    ClientConnected.Invoke(this, new ConnectionEventArgs(client));
                }
            }
            catch (Exception exc)
            {
                LastException = exc;

                logger.LogError(exc.ToString());
                try
                {
                    if (tcpClient != null && tcpClient.Connected)
                    {
                        tcpClient.Close();
                    }

                    if (client != null)
                    {
                        client.Close();
                    }
                }
                catch (Exception exc2)
                {
                    LastException = exc2;
                    logger.LogError(exc.ToString());
                }
            }

            AcceptConnections();
        }

        private void AcceptConnections()
        {
            try
            {
                if (!this.tcpServer.Server.IsBound)
                {
                    logger.LogInformation($"Restarting TCP Server on port {this.settings.port}...");
                    if (!IPAddress.TryParse(this.settings.host, out var ip))
                    {
                        ip = IPAddress.Any;
                    }

                    this.tcpServer = new TcpListener(new IPEndPoint(ip, this.settings.port));
                    StartAsync(CancellationToken.None);
                    return;
                }

                this.tcpServer.BeginAcceptTcpClient(OnClientConnected, this.tcpServer);
            }
            catch (Exception exc)
            {
                LastException = exc;
                logger.LogError(exc.ToString());
            }
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            var client = sender as INetworkClient;
            client.Disconnected -= Client_Disconnected;

            if (ClientDisconnected != null)
            {
                ClientDisconnected.Invoke(this, new ConnectionEventArgs(client));
            }

            try
            {
                client.Dispose();
            }
            catch (Exception exc)
            {
                LastException = exc;
                logger.LogError(exc.ToString());
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
