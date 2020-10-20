using System;

namespace RavenBot.Core
{
    public interface ITwitchBot : IDisposable
    {
        void Start();
        void Stop();
    }
}