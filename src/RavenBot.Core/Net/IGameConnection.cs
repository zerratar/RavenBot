using System.Threading.Tasks;

namespace RavenBot.Core.Net
{
    public interface IGameConnection
    {
        Task<bool> ConnectAsync(int port);
        void Disconnect();
        Task SendAsync(string msg);
        Task<string> ReceiveAsync();
        bool IsConnected { get; }

        void Reset();
    }
}