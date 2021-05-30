using RavenBot.Core;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Shinobytes.Network
{
    public abstract class NetworkClient : INetworkClient
    {
        protected readonly TcpClient Client;
        protected readonly Thread ReadThread;
        protected readonly Thread WriteThread;

        protected BinaryReader Reader;
        protected BinaryWriter Writer;
        protected bool IsConnected;

        private readonly byte[] readBuffer;

        private readonly ConcurrentQueue<DataPacket> writePackets = new ConcurrentQueue<DataPacket>();

        private SemaphoreSlim writeLock = new SemaphoreSlim(1);

        private bool disposed;

        public event EventHandler<DataPacket> DataReceived;
        public event EventHandler Disconnected;

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsReady { get; set; }
        public NetworkClient()
        {
            this.Client = new TcpClient();

            this.readBuffer = new byte[this.Client.ReceiveBufferSize];

            this.ReadThread = new Thread(Read);
            this.WriteThread = new Thread(Write);
        }

        public NetworkClient(TcpClient client)
        {
            this.Client = client;
            this.IsConnected = true;

            this.readBuffer = new byte[this.Client.ReceiveBufferSize];

            var networkStream = client.GetStream();
            this.Reader = new BinaryReader(networkStream);
            this.Writer = new BinaryWriter(networkStream);

            this.ReadThread = new Thread(Read);
            this.WriteThread = new Thread(Write);

            this.ReadThread.Start();
            this.WriteThread.Start();

        }

        private void Write()
        {
            while (!disposed && this.Client.Connected)
            {
                if (this.Writer == null)
                {
                    System.Threading.Thread.Sleep(50);
                    continue;
                }

                if (writePackets.TryDequeue(out var packet))
                {
                    try
                    {
                        this.Writer.Write(packet.Buffer, packet.Offset, packet.Length);
                        this.Writer.Flush();
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine(exc.ToString());
                        break;
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(50);
                }

                //writeLock.Wait(TimeSpan.FromSeconds(1));
            }

            Disconnect();
        }

        private void Read()
        {
            while (!disposed && this.Client.Connected)
            {
                System.Threading.Thread.Sleep(5);

                if (this.Reader == null)
                {
                    System.Threading.Thread.Sleep(50);
                    continue;
                }
                try
                {
                    var read = this.Reader.Read(readBuffer, 0, readBuffer.Length);
                    if (read > 0)
                    {
                        OnDataReceived(new DataPacket(readBuffer, 0, read));
                        continue;
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.ToString());
                }

                break;
            }

            Disconnect();
        }

        private void OnDataReceived(DataPacket dataPacket)
        {
            if (DataReceived != null)
            {
                DataReceived.Invoke(this, dataPacket);
            }
        }

        private void Disconnect()
        {
            if (!IsConnected || disposed)
            {
                return;
            }

            if (Disconnected != null)
            {
                Disconnected.Invoke(this, EventArgs.Empty);
            }

            IsConnected = false;
        }

        public void Dispose()
        {
            writeLock.Release();
            writeLock.Dispose();

            if (disposed)
            {
                return;
            }

            disposed = true;

            try
            {
                this.Client.Dispose();
            }
            catch { }

            Disconnect();
        }

        public void Send(byte[] data, int offset, int length)
        {
            writePackets.Enqueue(new DataPacket(data, offset, length));
            writeLock.Release();
        }

        public void Close()
        {
            if (disposed)
            {
                return;
            }

            if (this.Client.Connected)
            {
                this.Client.Close();
            }
        }
    }
}
