namespace RavenBot.Core.Net
{
    public class GameCommand : IGameCommand
    {

        public GameCommand(string destination, string command, params string[] args)
        {
            this.Destination = destination;
            this.Command = command;
            this.Args = args;
        }

        public string Destination { get; }

        public string Command { get; }

        public string[] Args { get; }
    }
}