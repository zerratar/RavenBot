namespace ROBot.Core.Twitch
{
    public interface ITwitchPubSubTokenRepository
    {
        PubSubToken AddOrUpdate(string userId, string userName, string token);
        PubSubToken GetByUserName(string channel);
        PubSubToken GetById(string userId);
    }
}
