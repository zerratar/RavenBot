namespace RavenBot.Core.Net
{
    public class GameMessage
    {
        public string Identifier { get; set; }
        public string Content { get; set; }
        public string CorrelationId { get; set; }
        public Ravenfall.Models.User Sender { get; set; }
    }
}