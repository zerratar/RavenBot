namespace RavenBot.Core.Chat.Twitch
{
    public class UserJoinedEvent
    {
        public string Name { get; }

        public UserJoinedEvent(string name)
        {
            Name = name;
        }
    }
}