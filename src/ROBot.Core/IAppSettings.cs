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
        string OpenAIAuthToken { get; set; }
        string DiscordAuthToken { get; set; }
    }
}