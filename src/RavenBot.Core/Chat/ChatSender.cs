namespace RavenBot.Core.Chat
{
    public class ChatSender
    {
        public ChatSender(string name, string nameColor)
        {
            Name = name;
            NameColor = nameColor;
        }

        public string Name { get; }
        public string NameColor { get; }
    }

}