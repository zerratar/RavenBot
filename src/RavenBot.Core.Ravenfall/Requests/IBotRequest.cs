using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public interface IBotRequest<TValueType>
    {
        Player Player { get; }
        TValueType Value { get; }
    }
}