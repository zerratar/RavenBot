namespace RavenBot.Core.Chat.Twitch
{
    public class UserLeftEvent
    {
        public string Name { get; }

        public UserLeftEvent(string name)
        {
            Name = name;
        }
    }
}