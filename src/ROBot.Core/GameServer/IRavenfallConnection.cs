﻿using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using RavenBot.Core.Ravenfall.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ROBot.Core.GameServer
{
    public interface IRavenfallConnection : IRavenfallClient, IDisposable
    {
        Guid InstanceId { get; }

        event EventHandler<RemoteGameSessionInfo> OnSessionInfoReceived;
        IGameSession Session { get; set; }
        IPEndPoint EndPoint { get; }
        string EndPointString { get; }
        IRavenfallApi Api { get; }
        IRavenfallApi Ref(string correlationId);
        IRavenfallApi this[string correlationid] { get; }
        IRavenfallApi this[ICommand cmd] { get; }
        void Close();
    }
}