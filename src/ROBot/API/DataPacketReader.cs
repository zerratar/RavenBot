using Shinobytes.Network;
using System;
using System.IO;

namespace ROBot.API
{
    public class DataPacketReader : IDisposable
    {
        private readonly MemoryStream memoryStream;
        private readonly BinaryReader reader;
        private bool disposed;

        public DataPacketReader(DataPacket dataPacket)
        {
            memoryStream = new MemoryStream(dataPacket.Buffer, dataPacket.Offset, dataPacket.Length);
            reader = new BinaryReader(memoryStream);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            try { reader.Dispose(); } catch { }
            try { memoryStream.Dispose(); } catch { }
            disposed = true;
        }

        public string ReadString()
        {
            var size = reader.ReadInt16();
            return System.Text.Encoding.UTF8.GetString(reader.ReadBytes(size));
        }
    }
}
