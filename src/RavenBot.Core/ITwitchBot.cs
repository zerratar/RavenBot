using System;
using System.Threading.Tasks;

namespace RavenBot.Core
{
    public interface ITwitchBot : IDisposable
    {
        Task StartAsync();
        void Stop();
    }
}