using Shinobytes.Network;

namespace Shinobytes.Network
{
    public record ServerPacket(string Type, DataPacket Data = null);
}
