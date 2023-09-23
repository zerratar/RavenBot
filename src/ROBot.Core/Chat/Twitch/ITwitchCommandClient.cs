using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch.PubSub;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Twitch
{
    public interface ITwitchCommandClient : IChatCommandClient
    {
        string GetPubSubActivationLink();
        string GetBotName();
        Task JoinChannelAsync(string channel);
        Task LeaveChannelAsync(string channel);
        bool InChannel(string name);
        PubSubState GetPubSubState(ICommandChannel channel);
    }
}