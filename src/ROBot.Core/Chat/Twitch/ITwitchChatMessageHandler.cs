using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Twitch
{
    public interface ITwitchChatMessageHandler : IDisposable
    {
        Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, TwitchLib.Client.Models.ChatMessage msg);
    }
}