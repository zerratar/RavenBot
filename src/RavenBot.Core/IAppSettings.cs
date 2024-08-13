namespace RavenBot.Core
{
    public interface IAppSettings
    {
        string TwitchBotUsername { get; set; }
        string TwitchBotAuthToken { get; set; }
        string TwitchChannel { get; set; }
        int Port { get; set; }
        string LogFile { get; set; }
        char? CommandIdentifier { get; }
        void Save();
    }
}