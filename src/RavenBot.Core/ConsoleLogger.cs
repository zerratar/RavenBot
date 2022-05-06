using System;

namespace RavenBot.Core
{
    public class ConsoleLogger : ILogger
    {
        private readonly object mutex = new object();

        public void WriteMessage(string message)
        {
            Write(message, "MSG", ConsoleColor.White);
        }

        public void WriteError(string error)
        {
            Write(error, "ERR", ConsoleColor.Red);
        }

        public void WriteDebug(string message)
        {
            Write(message, "DBG", ConsoleColor.Cyan);
        }

        public void WriteWarning(string message)
        {
            Write(message, "WRN", ConsoleColor.Yellow);
        }

        private void Write(string message, string tag, ConsoleColor foregroundColor)
        {
            lock (mutex)
            {
                var now = DateTime.Now;
                Console.ForegroundColor = foregroundColor;
                Console.WriteLine($"[{now:yyyy-MM-dd HH:mm:ss}][{tag}]: {message}");
                Console.ResetColor();
            }
        }
    }
}