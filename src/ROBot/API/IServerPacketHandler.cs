using Shinobytes.Network;
using System.Threading.Tasks;

namespace ROBot
{
    public interface IServerPacketHandler
    {
        Task HandleAsync(INetworkClient client, ServerPacket packet);
    }
}