using System.Collections.Concurrent;

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

    /// <summary>
    /// The last known state of every channel the bot joined or left, for the whole bot. Each game connection used to
    /// keep its own, filled only by joins that happened while it was connected, so a game that connected after the
    /// bot had joined its channel was never told and showed "Not in your chat" (October 2026).
    /// </summary>
    public static class ChannelStates
    {
        private static readonly ConcurrentDictionary<string, ChannelStateChangedEvent> states = new ConcurrentDictionary<string, ChannelStateChangedEvent>();

        public static void Set(ChannelStateChangedEvent state)
        {
            if (state?.ChannelName == null) return;
            states[state.ChannelName.ToLowerInvariant()] = state;
        }

        public static bool TryGet(string channel, out ChannelStateChangedEvent state)
        {
            state = null;
            return channel != null && states.TryGetValue(channel.ToLowerInvariant(), out state);
        }
    }

}