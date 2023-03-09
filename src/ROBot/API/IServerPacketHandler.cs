using Shinobytes.Network;
using System.Threading.Tasks;

namespace ROBot.API
{
    public interface IServerPacketHandler
    {
        Task HandleAsync(INetworkClient client, ServerPacket packet);
    }
}