using System;

namespace ROBot.Core
{
    public class AppSettings : IAppSettings
    {
        public AppSettings(string twitchBotUsername, string twitchBotAuthToken, string twitchBotAuthRefreshToken, string twitchBotClientId, DateTime twitchBotAuthTokenGenerated)
        {
            TwitchBotUsername = twitchBotUsername;
            TwitchBotAuthToken = twitchBotAuthToken;
            TwitchBotAuthRefreshToken = twitchBotAuthRefreshToken;
            TwitchBotClientId = twitchBotClientId;
            TwitchBotAuthTokenGenerated = twitchBotAuthTokenGenerated;
        }

        public string TwitchBotUsername { get; }
        public string TwitchBotAuthToken { get; set; }
        public string TwitchBotAuthRefreshToken { get; set; }
        public DateTime TwitchBotAuthTokenGenerated { get; set; }
        public string TwitchBotClientId { get; }
    }
}