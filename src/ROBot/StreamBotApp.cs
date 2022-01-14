using Microsoft.Extensions.Logging;
using ROBot.Core;
using ROBot.Core.GameServer;
using ROBot.Core.Twitch;
using ROBot.Ravenfall;
using System;
using System.Linq;

namespace ROBot
{

    // TODO: https://twitchtokengenerator.com/api/refresh/<refresh_token>

    public class StreamBotApp : IStreamBotApplication
    {
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly Shinobytes.Ravenfall.RavenNet.Core.IKernel kernel;
        private readonly IGameSessionManager sessionManager;
        private readonly IBotServer botServer;
        private readonly ITwitchCommandClient twitch;
        private Shinobytes.Ravenfall.RavenNet.Core.ITimeoutHandle timeoutHandle;
        private bool disposed;
        private DateTime startedDateTime;
        private long lastCmdCount;
        private int highestDelta;

        public StreamBotApp(
            Microsoft.Extensions.Logging.ILogger logger,
            Shinobytes.Ravenfall.RavenNet.Core.IKernel kernel,
            IGameSessionManager sessionManager,
            IBotServer ravenfall,
            ITwitchCommandClient twitch)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.sessionManager = sessionManager;
            this.botServer = ravenfall;
            this.twitch = twitch;

            sessionManager.SessionStarted += OnSessionStarted;
            sessionManager.SessionEnded += OnSessionEnded;
            sessionManager.SessionUpdated += OnSessionUpdated;
        }

        public void Run()
        {
            this.startedDateTime = DateTime.UtcNow;

            logger.LogInformation("[BOT] Application Started");

            logger.LogInformation("[BOT] Initializing Twitch Integration..");
            twitch.Start();

            logger.LogInformation("[BOT] Starting Bot Server..");
            botServer.Start();

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            try
            {
                var title = "Ravenfall Centralized Bot ";
                title += GetUptime();
                //title += GetConnectionsCount();
                //title += GetSessionCount();
                title += GetSessionsAndConnections();
                title += GetJoinedChannelCount();
                title += GetTrackedPlayerCount();
                title += GetCommandsPerSecond();
                Console.Title = title;
                if (this.disposed) { return; }
                this.timeoutHandle = this.kernel.SetTimeout(UpdateTitle, 1000);
            }
            catch
            {
                // Setting title on th is platform probably not supported.
            }
        }
        private string GetCommandsPerSecond()
        {
            var commandCount = twitch.GetCommandCount();
            double secondsSinceStart = (DateTime.UtcNow - startedDateTime).TotalSeconds;
            double delta = commandCount - lastCmdCount;
            double csSinceStart = Math.Round(commandCount / secondsSinceStart, 2);
            if (delta < csSinceStart)
            {
                delta = csSinceStart;
            }

            if (delta > highestDelta)
            {
                highestDelta = (int)delta;
            }

            lastCmdCount = commandCount;
            return "[Total: " + commandCount + " C/S: " + delta + " Hi: " + highestDelta + "] ";
        }

        private string GetTrackedPlayerCount()
        {
            var sessions = sessionManager.All();
            var userCount = 0;
            if (sessions.Count > 0)
            {
                userCount = sessions.Sum(x => x.UserCount);
            }

            return "[Players: " + userCount + "] ";
        }

        private string GetJoinedChannelCount()
        {
            var sessionCount = twitch.JoinedChannels().Count;
            return "[Joined: " + sessionCount + "] ";
        }

        private string GetSessionsAndConnections()
        {
            var connectionCount = botServer.AllConnections().Count;
            var sessionCount = sessionManager.All().Count;
            return "[Clients: " + sessionCount + "/" + connectionCount + "] ";
        }

        private string GetConnectionsCount()
        {
            var sessionCount = botServer.AllConnections().Count;
            return "[Connections: " + sessionCount + "] ";
        }

        private string GetSessionCount()
        {
            var sessionCount = sessionManager.All().Count;
            return "[Sessions: " + sessionCount + "] ";
        }

        private string GetUptime()
        {
            var elapsed = DateTime.UtcNow - startedDateTime;
            return "[Uptime: " + FormatTimeSpan(elapsed) + "] ";
        }

        private string FormatTimeSpan(TimeSpan elapsed)
        {
            var str = "";
            if (elapsed.Days > 0)
            {
                str += elapsed.Days + "d ";
            }
            if (elapsed.Hours > 0)
            {
                str += elapsed.Hours + "h ";
            }
            if (elapsed.Minutes > 0)
            {
                str += elapsed.Minutes + "m ";
            }

            str += elapsed.Seconds + "s";
            return str;
        }

        public void Shutdown()
        {
            logger.LogInformation("[BOT] Application Shutdown initialized.");
            Dispose();
        }

        public void Dispose()
        {
            if (disposed) return;
            sessionManager.SessionStarted -= OnSessionStarted;
            sessionManager.SessionEnded -= OnSessionEnded;
            sessionManager.SessionUpdated -= OnSessionUpdated;
            disposed = true;
            kernel.ClearTimeout(this.timeoutHandle);
            twitch.Dispose();
            botServer.Dispose();
            try { kernel.Stop(); } catch { }
        }

        private void OnSessionUpdated(object sender, GameSessionUpdateEventArgs e)
        {
            if (e.OldName != e.Session.Name)
            {
                logger.LogWarning("[RVNFLL] Game Session Name Changed (OldName: " + e.OldName + " New: " + e.Session.Name + ")");
                twitch.LeaveChannel(e.OldName);
                twitch.JoinChannel(e.Session.Name);
                return;
            }

            if (!twitch.InChannel(e.Session.Name))
            {
                twitch.JoinChannel(e.Session.Name);
            }
        }

        private void OnSessionStarted(object sender, IGameSession session)
        {
            logger.LogDebug("[RVNFLL] Game Session Started (Name:" + session.Name + ")");
            twitch.JoinChannel(session.Name);
        }

        private void OnSessionEnded(object sender, IGameSession session)
        {
            logger.LogDebug("[RVNFLL] Game Session Ended (Name: " + session.Name + ")");
            twitch.LeaveChannel(session.Name);
        }
    }
}
