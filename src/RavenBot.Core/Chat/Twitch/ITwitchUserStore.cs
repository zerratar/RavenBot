namespace RavenBot.Core.Chat.Twitch
{
    public interface ITwitchUserStore
    {
        ITwitchUser Get(string username);
    }
}