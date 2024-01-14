using RavenBot.Core;

namespace RavenBot
{
    public class DefaultChannelProvider : IChannelProvider
    {
        private readonly IAppSettings settings;

        public DefaultChannelProvider(IAppSettings appSettings)
        {
            this.settings = appSettings;
        }
        public string Get()
        {
            return settings.TwitchChannel;
        }
    }
}