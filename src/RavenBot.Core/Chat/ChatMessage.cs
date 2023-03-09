namespace RavenBot.Core.Chat
{
    public class ChatMessage
    {
        public ChatMessage(ChatSender sender, ChatMessagePart[] message)
        {
            Sender = sender;
            Message = message;
        }

        public ChatSender Sender { get; }
        public ChatMessagePart[] Message { get; }
    }
}