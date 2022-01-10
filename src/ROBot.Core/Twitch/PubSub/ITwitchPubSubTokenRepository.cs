namespace ROBot.Core.Twitch
{
    public interface ITwitchPubSubTokenRepository
    {
        PubSubToken AddOrUpdate(string userId, string userName, string token);
        PubSubToken AddOrUpdate(string userId, string userName, string token, bool? badAuth);
        PubSubToken GetByUserName(string channel);
        PubSubToken GetById(string userId);
        PubSubToken GetToken(string channel, string userID);

    }
}
