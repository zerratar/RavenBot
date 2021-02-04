namespace RavenBot.Core.Net
{
    public class GamePacket
    {
        public GamePacket(string destination, string id, string format, string[] args)
        {
            this.Destination = destination;
            this.Id = id;
            this.Format = format;
            this.Args = args;
        }

        public string Destination { get; }
        public string Id { get; }
        public string Format { get; }
        public string[] Args { get; }
    }
}