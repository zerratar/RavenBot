using Shinobytes.Network;
using System.IO;
using System.Text;

namespace Shinobytes.Network
{
    public class BinaryServerPacketSerializer : IServerPacketSerializer
    {
        public ServerPacket Deserialize(DataPacket packet)
        {
            using (var mem = new System.IO.MemoryStream(packet.Buffer, packet.Offset, packet.Length))
            using (var reader = new BinaryReader(mem))
            {
                var typeLen = reader.ReadByte();
                var typeData = reader.ReadBytes(typeLen);
                var type = UTF8Encoding.UTF8.GetString(typeData, 0, typeLen);

                var bodyDataLen = reader.ReadInt16();
                if (bodyDataLen > 0)
                {
                    var bodyData = reader.ReadBytes(bodyDataLen);
                    return new ServerPacket(type, new DataPacket(bodyData, 0, bodyDataLen));
                }
                
                return new ServerPacket(type, null);
            }
        }

        public DataPacket Serialize(ServerPacket packet)
        {
            using (var mem = new System.IO.MemoryStream())
            using (var writer = new BinaryWriter(mem))
            {
                var typeData = UTF8Encoding.UTF8.GetBytes(packet.Type);
                writer.Write((byte)typeData.Length);
                writer.Write(typeData);

                if (packet.Data != null)
                {
                    writer.Write((short)packet.Data.Length);
                    if (packet.Data.Length > 0)
                    {
                        writer.Write(packet.Data.Buffer);
                    }
                }
                else
                {
                    writer.Write((short)0);
                }

                var data = mem.ToArray();
                return new DataPacket(data, 0, data.Length);
            }
        }
    }
}
