namespace RavenBot.Core.Chat
{
    public class ChatMessagePart
    {
        public ChatMessagePart(string type, string value)
        {
            Type = type;
            Value = value;
        }

        public string Type { get; }
        public string Value { get; }
    }
}