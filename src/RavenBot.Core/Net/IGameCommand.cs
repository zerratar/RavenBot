namespace RavenBot.Core.Net
{
    public interface IGameCommand
    {
        string Destination { get; }
        string Command { get; }
        string[] Args { get; }
    }
}