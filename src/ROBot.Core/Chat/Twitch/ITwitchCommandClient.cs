namespace ROBot.Core.Chat.Twitch
{
    public interface ITwitchCommandClient : IChatCommandClient
    {
        void JoinChannel(string channel);
        void LeaveChannel(string channel);
        bool InChannel(string name);
    }
}