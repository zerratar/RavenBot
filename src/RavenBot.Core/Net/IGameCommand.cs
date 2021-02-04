namespace RavenBot.Core.Net
{
    public interface IGameCommand
    {
        string Receiver { get; }
        string Identifier { get; }
        string Format { get; }
        string[] Args { get; }
    }
}