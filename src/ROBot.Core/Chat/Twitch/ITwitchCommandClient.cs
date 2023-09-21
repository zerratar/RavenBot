using System.Threading.Tasks;

namespace ROBot.Core.Chat.Twitch
{
    public interface ITwitchCommandClient : IChatCommandClient
    {
        Task JoinChannelAsync(string channel);
        Task LeaveChannelAsync(string channel);
        bool InChannel(string name);
    }
}