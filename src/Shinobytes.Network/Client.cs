using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Shinobytes.Network
{
    public class Client : NetworkClient, IClient
    {
        public Client()
        {
        }
        
        bool IClient.Connected => this.Client != null && this.Client.Connected;

        public async Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            try
            {
                await Client.ConnectAsync(host, port, cancellationToken);
                var networkStream = Client.GetStream();
                this.Reader = new BinaryReader(networkStream);
                this.Writer = new BinaryWriter(networkStream);                
                this.IsConnected = true;
                this.ReadThread.Start();
                this.WriteThread.Start();
                return true;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
            return false;
        }
    }
}
