using System;
using System.Threading.Tasks;

namespace RavenBot.Core.Net
{
    public interface IGameClient : IDisposable
    {
        Task<bool> ProcessAsync(int serverPort);
        IGameClientSubcription Subscribe(string type, System.Action<GameMessageResponse> onCommand);
        Task SendAsync(string message);
        bool IsConnected { get; }

        event EventHandler Connected;
    }
}