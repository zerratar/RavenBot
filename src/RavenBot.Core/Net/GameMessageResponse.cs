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
            if (!string.IsNullOrEmpty(format) && format.StartsWith("%["))
            {
                format = MessageUtilities.TryExtractCategory(format, out category);
                format = MessageUtilities.TryExtractTags(format, out tags);
            }

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
}