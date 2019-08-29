using System;

namespace RavenBot.Core
{
    public interface ICommandListener : IDisposable
    {
        void Start();
        void Stop();
    }
}