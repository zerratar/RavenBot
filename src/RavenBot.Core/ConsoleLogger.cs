using System;

namespace RavenBot.Core
{
    public class FileLogger : ILogger
    {
        private readonly object mutex = new object();

        public string Path { get; }

        public FileLogger(IAppSettings appSettings)
        {
            this.Path = appSettings.LogFile;
            if (string.IsNullOrEmpty(Path))
            {
                Path = "log.txt";
            }
        }

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

                try
                {
                    if (String.IsNullOrEmpty(Path)) return;

                    using (var writer = new System.IO.StreamWriter(Path, true))
                    {
                        writer.WriteLine($"[{now:yyyy-MM-dd HH:mm:ss}][{tag}]: {message}");
                    }
                }
                catch (Exception exc)
                {
                }
            }
        }
    }

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