namespace ROBot.Core.Twitch
{
    public class PubSubToken
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Token { get; set; }
        public bool? BadAuth { get; set; }
    }
}
