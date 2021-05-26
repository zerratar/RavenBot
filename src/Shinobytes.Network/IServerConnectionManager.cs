using System;
using System.Collections.Generic;

namespace Shinobytes.Network
{
    public interface IServerConnectionManager : IDisposable
    {
        IReadOnlyList<INetworkClient> All();
        INetworkClient Get(Guid id);
        void Add(INetworkClient client);
        void Remove(INetworkClient client);
    }
}
