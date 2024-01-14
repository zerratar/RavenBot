namespace RavenBot.Core
{
    public class AppSettings : IAppSettings
    {
        public AppSettings(string twitchBotUsername, string twitchBotAuthToken, string twitchChannel)
        {
            TwitchBotUsername = twitchBotUsername;
            TwitchBotAuthToken = twitchBotAuthToken;
            TwitchChannel = twitchChannel;
        }

        public string TwitchBotUsername { get; set; }
        public string TwitchBotAuthToken { get; set; }

        public string TwitchChannel { get; set; }

        public void Save()
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            System.IO.File.WriteAllText("settings.json", json);
        }
    }
}