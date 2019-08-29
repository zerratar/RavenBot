using System.Net.WebSockets;

namespace RavenBot.Core.Net.WebSocket
{
    public interface IPacketDataSerializer
    {
        T Serialize<T>(Packet data);
        byte[] Deserialize(Packet packet);
        Packet Deserialize<T>(T data);
        Packet Deserialize(byte[] buffer, int offset, int size, WebSocketMessageType messageType, bool endOfMessage);
    }
}