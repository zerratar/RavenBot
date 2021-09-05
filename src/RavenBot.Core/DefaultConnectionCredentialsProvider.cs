using TwitchLib.Client.Models;

namespace RavenBot.Core
{
    public class DefaultConnectionCredentialsProvider : IConnectionCredentialsProvider
    {
        private readonly IAppSettings settings;
        public DefaultConnectionCredentialsProvider(IAppSettings settings)
        {
            this.settings = settings;
        }

        public ConnectionCredentials Get()
        {
            return new ConnectionCredentials(
                settings.TwitchBotUsername,
                settings.TwitchBotAuthToken);
        }
    }
}