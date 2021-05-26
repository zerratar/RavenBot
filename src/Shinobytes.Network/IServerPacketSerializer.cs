using Shinobytes.Network;

namespace Shinobytes.Network
{
    public interface IServerPacketSerializer
    {
        ServerPacket Deserialize(DataPacket packet);
        DataPacket Serialize(ServerPacket packet);
    }
}
