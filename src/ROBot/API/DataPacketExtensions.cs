using Shinobytes.Network;

namespace ROBot.API
{
    public static class DataPacketExtensions
    {
        public static DataPacketReader GetReader(this DataPacket datapacket)
        {
            return new DataPacketReader(datapacket);
        }
    }
}
