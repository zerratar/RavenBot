using System;

namespace Shinobytes.Network
{
    public class ConnectionEventArgs : EventArgs
    {
        public INetworkClient Client { get; }
        public ConnectionEventArgs(INetworkClient client)
        {
            this.Client = client;
        }
    }
}
