using ROBot.Core.Chat;
using ROBot.Core.GameServer;
using System.Threading.Tasks;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Chat.Twitch
{
    public interface ITwitchCommandController : IChatCommandController
    {
        Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, TwitchLib.Client.Models.ChatCommand cmd, TwitchLib.Client.Models.ChatMessage msg);
        Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, ChatMessage msg);
        Task<bool> HandleAsync(IBotServer game, IChatCommandClient chat, OnChannelPointsRewardRedeemedArgs reward);
    }
}