namespace RavenBot.Core
{
    public interface ILogger
    {
        void WriteMessage(string message);
        void WriteError(string error);
        void WriteDebug(string message);
        void WriteWarning(string message);
    }

    public static class Logger
    {
        public static ILogger Console => new ConsoleLogger();
    }
}