using Microsoft.Extensions.Logging;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace ROBot
{
    public class PersistedConsoleLogger : ILogger, IDisposable
    {
        const string logsDir = "../logs";
        const double logsLifespanDays = 7;

        private readonly ConsoleLogger logger;
        private readonly IMessageBus messageBus;
        private readonly List<IMessageBusSubscription> subscriptions;
        private readonly object mutex = new object();
        private readonly List<string> messages = new List<string>();

        private readonly int maxMessageStack = 1000;
        private DateTime lastSave = DateTime.UtcNow;
        private TimeSpan maxTimeBetweenSave = TimeSpan.FromSeconds(5);
        private LogLevel lastLogLevel = LogLevel.Debug;
        private string lastTag;
        public PersistedConsoleLogger(IMessageBus messageBus)
        {
            this.logger = new ConsoleLogger();
            this.messageBus = messageBus;
            this.subscriptions = new List<IMessageBusSubscription>();
            this.subscriptions.Add(this.messageBus.Subscribe<string>(MessageBus.MessageBusException, str => this.LogError(str)));
            this.subscriptions.Add(this.messageBus.Subscribe("exit", () => TrySaveLogToDisk()));
        }

        public void TrySaveLogToDisk()
        {
            lock (mutex)
            {
                try
                {
                    lastSave = DateTime.UtcNow;
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
            subscriptions.ForEach(x => x.Unsubscribe());
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.logger.Log<TState>(logLevel, eventId, state, exception, formatter);
            var message = formatter != null ? formatter(state, exception) : state.ToString();

            var appendNewLine = lastLogLevel != logLevel;
            if (!string.IsNullOrEmpty(message))
            {
                var bracketEnd = message.IndexOf(']');
                var bracketStart = message.IndexOf('[');
                if (bracketStart != -1 && bracketStart > bracketEnd)
                {
                    var tag = message.Split(']')[0].Split('[')[1];
                    if (tag != lastTag)
                    {
                        appendNewLine = true;
                    }
                    lastTag = tag;
                }
            }

            PersistLine("{" + logLevel + "}: " + message, appendNewLine);
            lastLogLevel = logLevel;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PersistLine(string str, bool appendNewLine = false)
        {
            if (appendNewLine) AddMessage("");
            AddMessage($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss K}]: {str}");
        }

        private void AddMessage(string str)
        {
            lock (mutex)
            {
                messages.Add(str);

                if (messages.Count > maxMessageStack || (DateTime.UtcNow - lastSave) >= maxTimeBetweenSave)
                {
                    TrySaveLogToDisk();
                    messages.Clear();
                }
            }
        }
    }
}
