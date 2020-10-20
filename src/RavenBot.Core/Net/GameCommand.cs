namespace RavenBot.Core.Net
{
    public class GameCommand : IGameCommand
    {

        public GameCommand(string destination, string id, string message, params string[] args)
        {
            this.Destination = destination;
            this.Identifier = id;
            this.Format = message;
            this.Args = args;
        }

        public string Destination { get; }
        public string Identifier { get; }
        public string Format { get; }
        public string[] Args { get; }
    }
}