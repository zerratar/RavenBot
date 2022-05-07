using Microsoft.Extensions.Logging;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace ROBot
{
    public class PersistedConsoleLogger : ILogger, IDisposable
    {
        const string logsDir = "../logs";
        const double logsLifespanDays = 7;

        private readonly ConsoleLogger logger;
        private readonly IMessageBus messageBus;
        private readonly IMessageBusSubscription subscription;
        private readonly object mutex = new object();
        private readonly List<string> messages = new List<string>();
        private readonly int maxMessageStack = 1000;

        public PersistedConsoleLogger(IMessageBus messageBus)
        {
            this.logger = new ConsoleLogger();
            this.messageBus = messageBus;
            this.subscription = this.messageBus.Subscribe("exit", () =>
            {
                TrySaveLogToDisk();
            });
        }

        public void TrySaveLogToDisk()
        {
            lock (mutex)
            {
                try
                {
                    var fn = DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log";

                    if (!System.IO.Directory.Exists(logsDir))
                    {
                        System.IO.Directory.CreateDirectory(logsDir);
                    }

                    System.IO.File.AppendAllLines(System.IO.Path.Combine(logsDir, fn), messages);
                }
                catch (System.Exception exc)
                {
                    System.Console.WriteLine(exc.ToString());
                }
                finally
                {
                    CleanupLogs();
                }
            }
        }

        private void CleanupLogs()
        {
            if (!System.IO.Directory.Exists(logsDir))
            {
                return;
            }

            var logs = System.IO.Directory.GetFiles(logsDir, "*.log");
            foreach (var log in logs)
            {
                try
                {
                    var fi = new FileInfo(log);
                    if (fi.CreationTimeUtc >= DateTime.UtcNow.AddDays(logsLifespanDays))
                    {
                        fi.Delete();
                    }
                }
                catch { }
            }
        }

        public void Dispose()
        {
            subscription.Unsubscribe();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.logger.Log<TState>(logLevel, eventId, state, exception, formatter);
            var message = formatter != null ? formatter(state, exception) : state.ToString();
            PersistLine("{" + logLevel + "}: " + message);
        }

        public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state) => logger.BeginScope<TState>(state);

        public void Write(string message)
        {
            this.logger.Write(message);
            PersistLine(message);
        }

        public void WriteLine(string message)
        {
            this.logger.WriteLine(message);
            PersistLine(message);
        }

        private void PersistLine(string v)
        {
            AddMessage($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss K}]: {v}");
        }

        private void AddMessage(string str)
        {
            lock (mutex)
            {
                messages.Add(str);

                if (messages.Count > maxMessageStack)
                {
                    TrySaveLogToDisk();
                    messages.Clear();
                }
            }
        }
    }
}
