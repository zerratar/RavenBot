using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public interface IBotRequest<TValueType>
    {
        User Player { get; }
        TValueType Value { get; }
    }
}