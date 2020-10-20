namespace RavenBot.Core.Net
{
    public interface IGameCommand
    {
        string Destination { get; }
        string Identifier { get; }
        string Format { get; }
        string[] Args { get; }
    }
}