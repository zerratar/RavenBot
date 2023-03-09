using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Chat
{
    public interface IChatCommandHandler : IDisposable
    {
        bool RequiresBroadcaster { get; set; }
        Task HandleAsync(IBotServer game, IChatCommandClient twitch, ICommand cmd);
    }
}
