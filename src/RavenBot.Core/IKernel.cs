using System;
using System.Collections.Concurrent;

namespace RavenBot.Core
{
    public interface IKernel
    {
        ITimeoutHandle SetTimeout(Action action, int timeoutMilliseconds);
        void ClearTimeout(ITimeoutHandle discordBroadcast);

        void Start();
        void Stop();
        bool Started { get; }
    }
}