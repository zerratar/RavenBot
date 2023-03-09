using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Chat.Twitch
{
    public class TwitchCredentialsProvider : ITwitchCredentialsProvider
    {
        private readonly IAppSettings settings;

        public TwitchCredentialsProvider(IAppSettings settings)
        {
            this.settings = settings;
        }

        public ConnectionCredentials Get()
        {
            return new ConnectionCredentials(
                settings.TwitchBotUsername,
                settings.TwitchBotAuthToken);
        }

        //public Task RefreshAccessToken()
        //{
        //    var refreshUrl = "https://twitchtokengenerator.com/api/refresh/" + settings.TwitchBotAuthRefreshToken;
        //}
    }
}