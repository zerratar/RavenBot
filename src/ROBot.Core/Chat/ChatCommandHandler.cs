using RavenBot.Core.Handlers;
using ROBot.Core.Chat;
using ROBot.Core.GameServer;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Chat
{
    public abstract class ChatCommandHandler : IChatCommandHandler
    {
        public virtual void Dispose()
        {
        }

        public bool RequiresBroadcaster { get; set; }
        public abstract Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd);
    }
}