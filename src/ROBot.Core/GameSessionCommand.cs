namespace ROBot.Core
{
    public class GameSessionCommand : IGameSessionCommand
    {
        public GameSessionCommand(IGameSession session, string destination, string id, string message, params string[] args)
        {
            this.Session = session;
            this.Receiver = destination;
            this.Identifier = id;
            this.Format = message;
            this.Args = args;
        }
        public IGameSession Session { get; }
        public string Receiver { get; }
        public string Identifier { get; }
        public string Format { get; }
        public string[] Args { get; }
    }
}
