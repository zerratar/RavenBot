using RavenBot.Core.Handlers;
using ROBot.Core.Chat;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Chat
{
    public abstract class ChatCommandHandler : IChatCommandHandler
    {
        public virtual void Dispose()
        {
        }

        public virtual IReadOnlyList<ChatCommandInput> Inputs { get; }
        public virtual string Description { get; }
        public bool RequiresBroadcaster { get; set; }
        public abstract Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd);
    }
}