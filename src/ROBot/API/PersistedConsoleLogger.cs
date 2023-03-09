using Microsoft.Extensions.Logging;
using Shinobytes.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ROBot.API
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
        public PersistedConsoleLogger(IMessageBus messageBus)
        {
            logger = new ConsoleLogger();
            this.messageBus = messageBus;
            subscriptions = new List<IMessageBusSubscription>();
            subscriptions.Add(this.messageBus.Subscribe<string>(MessageBus.MessageBusException, str => this.LogError(str)));
            subscriptions.Add(this.messageBus.Subscribe("exit", () => TrySaveLogToDisk()));
        }

        public void TrySaveLogToDisk()
        {
            lock (mutex)
            {
                try
                {
                    lastSave = DateTime.UtcNow;
                    var fn = DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log";
                    var fullPathToFile = Path.Combine(logsDir, fn);

                    if (!Directory.Exists(logsDir))
                    {
                        Directory.CreateDirectory(logsDir);
                    }

                    //doesn't lock file when writing
                    using (var outStream = new FileStream(fullPathToFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var output = new StreamWriter(outStream))
                    {
                        foreach (var message in messages)
                        {
                            output.WriteLine(message);
                        }
                    }

                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.ToString());
                }
                finally
                {
                    CleanupLogs();
                }
            }
        }

        private void CleanupLogs()
        {
            if (!Directory.Exists(logsDir))
            {
                return;
            }

            var logs = Directory.GetFiles(logsDir, "*.log");
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
            logger.Log(logLevel, eventId, state, exception, formatter);
            var message = formatter != null ? formatter(state, exception) : state.ToString();
            PersistLine(message, logLevel);
        }

        public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state) => logger.BeginScope(state);

        public void Write(string message)
        {
            logger.Write(message);
            PersistLine(message);
        }

        public void WriteLine(string message)
        {
            logger.WriteLine(message);
            PersistLine(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PersistLine(string str, LogLevel loglevel = LogLevel.None)
        {
            AddMessage(new LogEntry
            {
                LogDateTime = DateTime.UtcNow,
                LogLevel = loglevel,
                Message = str
            });
        }

        private void AddMessage(LogEntry log)
        {
            lock (mutex)
            {
                messages.Add(JsonSerializer.Serialize(log));

                if (messages.Count > maxMessageStack || DateTime.UtcNow - lastSave >= maxTimeBetweenSave)
                {
                    TrySaveLogToDisk();
                    messages.Clear();
                }
            }
        }

        //TODO export out as model, copy also exisit in ServerServices in RavenNest.Blazor.Services
        private class LogEntry
        {
            public DateTime LogDateTime { get; set; }
            public LogLevel LogLevel { get; set; }
            public string Message { get; set; }
        }
    }
}
