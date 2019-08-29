namespace RavenBot.Core.Net
{
    public interface IGameCommand
    {
        string CorrelationId { get; }
        string Destination { get; }
        string Command { get; }
        string[] Args { get; }
    }
}