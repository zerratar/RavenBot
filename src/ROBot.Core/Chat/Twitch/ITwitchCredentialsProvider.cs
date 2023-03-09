using TwitchLib.Client.Models;

namespace ROBot.Core.Chat.Twitch
{
    public interface ITwitchCredentialsProvider
    {
        ConnectionCredentials Get();
    }
}