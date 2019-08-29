using System;
using System.Threading.Tasks;

namespace RavenBot.Core.Net
{
    public interface IGameClient2 : IGameClient { }
    public interface IGameClient3 : IGameClient { }
    public interface IGameClient4 : IGameClient { }

    public interface IGameClient : IDisposable
    {
        Task<bool> ProcessAsync(int serverPort);
        IGameClientSubcription Subscribe(string cmdIdentifier, System.Action<IGameCommand> onCommand);
        Task SendAsync(string message);
        bool IsConnected { get; }
        event EventHandler Connected;
    }
}