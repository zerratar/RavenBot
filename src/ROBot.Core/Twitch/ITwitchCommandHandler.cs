using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Twitch
{
    public interface ITwitchCommandHandler : IDisposable
    {
        bool RequiresBroadcaster { get; set; }
        Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd);
    }
}