using System.Numerics;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall
{
    public interface IRavenfallClient
    {
        IRavenfallApi Api { get; }
        IRavenfallApi Ref(string correlationId);
        IRavenfallApi this[string correlationid] { get; }
        IRavenfallApi this[ICommand cmd] { get; }
        Task<bool> ProcessAsync(int serverPort);
    }
}
