namespace RavenBot.Core.Net
{
    public class GameCommand : IGameCommand
    {

        public GameCommand(string correlationId, string destination, string command, params string[] args)
        {
            CorrelationId = correlationId;
            this.Destination = destination;
            this.Command = command;
            this.Args = args;
        }

        public string CorrelationId { get; }

        public string Destination { get; }

        public string Command { get; }

        public string[] Args { get; }
    }
}