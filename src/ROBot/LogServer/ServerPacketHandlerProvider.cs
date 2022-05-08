using RavenBot.Core;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using TwitchLib.Client.Exceptions;

namespace ROBot
{
    public class ServerPacketHandlerProvider : IServerPacketHandlerProvider
    {
        private readonly Shinobytes.Ravenfall.RavenNet.Core.IoC ioc;
        private bool initialized;
        private ConcurrentDictionary<string, Type> handlers = new ConcurrentDictionary<string, Type>();

        public ServerPacketHandlerProvider(Shinobytes.Ravenfall.RavenNet.Core.IoC ioc)
        {
            this.ioc = ioc;
        }

        public IServerPacketHandler Get(string type)
        {
            Initialize();

            var key = type.ToLower();

            if (handlers.TryGetValue(key, out var handlerType))
            {
                return ioc.Resolve(handlerType) as IServerPacketHandler;
            }

            return null;
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            var packetHandlers = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsAssignableTo(typeof(IServerPacketHandler))).ToArray();
            foreach (var ph in packetHandlers)
            {
                ioc.RegisterShared(ph);
                var key = ph.Name.Replace("PacketHandler", "").ToLower();
                handlers[key] = ph;
            }

            initialized = true;
        }
    }


}