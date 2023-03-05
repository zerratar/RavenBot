using RavenBot.Core.Net;

namespace ROBot.Core
{
    public interface IGameSessionCommand : IGameCommand
    {
        IGameSession Session { get; }
    }
}
