using Shinobytes.Network;

namespace ROBot.LogServer.PacketHandlers
{
    public static class DataPacketExtensions
    {
        public static DataPacketReader GetReader(this DataPacket datapacket)
        {
            return new DataPacketReader(datapacket);
        }
    }
}
