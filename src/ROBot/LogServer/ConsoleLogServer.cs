﻿using Microsoft.Extensions.Logging;
using Shinobytes.Network;
using Shinobytes.Ravenfall.RavenNet.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ROBot
{
    public interface IExtendedLogger : ILogger
    {

    }

    public class ConsoleLogServer : IExtendedLogger, IDisposable
    {

        const string logsDir = "../logs";
        const double logsLifespanDays = 7;

        private readonly ConsoleLogger logger;
        private readonly IMessageBus messageBus;

        private readonly object mutex = new object();
        private readonly List<string> messages = new List<string>();

        public ConsoleLogServer(
            IMessageBus messageBus)
        {
            this.logger = new ConsoleLogger();
            this.messageBus = messageBus;
            SetupServer();
        }
        private void SetupServer()
        {
            this.messageBus.Subscribe("exit", () =>
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

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.logger.Log<TState>(logLevel, eventId, state, exception, formatter);
            var message = formatter != null ? formatter(state, exception) : state.ToString();
        }

        public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state) => logger.BeginScope<TState>(state);

        public void Write(string message)
        {
            this.logger.Write(message);
        }

        public void WriteLine(string message)
        {
            this.logger.WriteLine(message);
        }

        public void Dispose()
        {
            
        }
    }
}
