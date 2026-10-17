using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RavenBot.Core.Net
{
    public class TcpGameConnection : IGameConnection, IDisposable
    {
        private readonly ILogger logger;
        // one writer at a time, concurrent writes on a StreamWriter throw and drop the link.
        private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;

        public TcpGameConnection(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<bool> ConnectAsync(int port)
        {
            try
            {
                this.client = new TcpClient();
                await this.client.ConnectAsync(IPAddress.Loopback, port);
                if (this.client.Connected)
                {
                    this.reader = new StreamReader(this.client.GetStream());
                    this.writer = new StreamWriter(this.client.GetStream());
                    return true;
                }
            }
            catch (WebException) { }
            catch (SocketException) { }
            catch (Exception exc)
            {
                this.logger.WriteError(exc.ToString());
            }

            this.Disconnect();
            return false;
        }

        public void Disconnect()
        {
            try
            {
                if (this.client != null && this.client.Connected)
                {
                    this.client.Close();
                }
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.ToString());
            }

            this.client = null;
        }

        public async Task SendAsync(string msg)
        {
            await this.writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var w = this.writer;
                if (w != null && w.BaseStream.CanWrite)
                {
                    await w.WriteLineAsync(msg).ConfigureAwait(false);
                    await w.FlushAsync().ConfigureAwait(false);
                    return;
                }
            }
            catch (Exception exc)
            {
                this.logger.WriteError(exc.ToString());
            }
            finally
            {
                this.writeLock.Release();
            }

            Disconnect();
        }

        public async Task<string> ReceiveAsync()
        {
            try
            {
                if (this.reader.BaseStream.CanRead)
                {
                    var data = await this.reader.ReadLineAsync();
                    if (data != null)
                    {
                        return data;
                    }
                }
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.ToString());
            }

            Disconnect();
            return null;
        }

        public bool IsConnected => this.client != null && this.client.Client.Connected;

        public void Dispose()
        {
            Disconnect();
        }

        public void Reset()
        {
            try
            {
                Disconnect();
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.ToString());
            }

            this.client = new TcpClient();
        }
    }
}