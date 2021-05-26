using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Shinobytes.Network
{
    public class ServerConnectionManager : IServerConnectionManager
    {
        private readonly Dictionary<Guid, INetworkClient> clients
            = new Dictionary<Guid, INetworkClient>();

        private readonly object mutex = new object();

        public void Add(INetworkClient client)
        {
            lock (mutex)
            {
                clients[client.Id] = client;
            }
        }

        public IReadOnlyList<INetworkClient> All()
        {
            lock (mutex)
            {
                List<INetworkClient> c = new List<INetworkClient>();
                foreach (var key in clients.Keys)
                {
                    if (clients.TryGetValue(key, out var client))
                    {
                        c.Add(client);
                    }
                }

                return c;
            }
        }

        public void Dispose()
        {
            lock (mutex)
            {
                foreach (var c in clients.Values)
                {
                    c.Dispose();
                }

                clients.Clear();
            }
        }

        public INetworkClient Get(Guid id)
        {
            lock (mutex)
            {
                if (clients.TryGetValue(id, out var client))
                {
                    return client;
                }

                return null;
            }
        }

        public void Remove(INetworkClient client)
        {
            lock (mutex)
            {
                clients.Remove(client.Id);
                client.Dispose();
            }
        }
    }
}
