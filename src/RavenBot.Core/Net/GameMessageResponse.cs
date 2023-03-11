using System;

namespace RavenBot.Core.Net
{
    public class GameMessageResponse
    {
        public GameMessageResponse(
            string identifier,
            GameMessageRecipent recipent,
            string format,
            object[] args,
            string[] tags,
            string category,
            string correlationId)
        {
            this.Identifier = identifier;
            this.Format = format;
            this.Recipent = recipent;
            this.Args = args;
            this.Tags = tags;
            this.Category = category;
            this.CorrelationId = correlationId;
        }

        public string Identifier { get; }
        public GameMessageRecipent Recipent { get; }
        public string Format { get; }
        public object[] Args { get; }
        public string[] Tags { get; }
        public string Category { get; }
        public string CorrelationId { get; }
    }


    public class GameMessageRecipent
    {
        public GameMessageRecipent(Guid userId, string platform, string platformId, string platformUserName)
        {
            UserId = userId;
            Platform = platform;
            PlatformId = platformId;
            PlatformUserName = platformUserName;
        }

        public Guid UserId { get; }
        public string Platform { get; }
        public string PlatformId { get; }
        public string PlatformUserName { get; }
    }

    public class GameMessage
    {
        public string Identifier { get; set; }
        public string Content { get; set; }
        public string CorrelationId { get; set; }
        public Ravenfall.Models.User Sender { get; set; }
    }
}