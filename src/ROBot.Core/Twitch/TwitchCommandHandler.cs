using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Twitch
{
    public abstract class TwitchCommandHandler : ITwitchCommandHandler
    {
        public virtual void Dispose()
        {
        }

        public abstract Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd);
    }
}