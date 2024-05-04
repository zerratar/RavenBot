namespace RavenBot.Core.Chat.Twitch
{
    public class ChannelStateChangedEvent
    {
        public string Platform { get; }
        public string ChannelName { get; }
        public bool InChannel { get; }
        public string Message { get; }
        public ChannelStateChangedEvent(string platform, string channel, bool inChannel, string message)
        {
            this.Platform = platform;
            this.ChannelName = channel;
            this.InChannel = inChannel;
            this.Message = message;
        }
    }

}