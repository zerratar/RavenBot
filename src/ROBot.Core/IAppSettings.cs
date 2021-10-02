using System;

namespace ROBot.Core
{
    public interface IAppSettings
    {
        string TwitchBotUsername { get; }
        string TwitchBotAuthToken { get; set; }
        string TwitchBotAuthRefreshToken { get; set; }
        string TwitchBotClientId { get; }
        DateTime TwitchBotAuthTokenGenerated { get; set; }
    }
}