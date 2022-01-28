using Microsoft.Extensions.Logging;
using ROBot.Core;
using ROBot.Core.GameServer;
using ROBot.Core.Twitch;
using ROBot.Ravenfall;
using System;
using System.Linq;
using System.Net.Http;

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
        private int detailsDelayTimer;
        private StreamBotStats stats;

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

                // Ping RavenNest with details.

                SendDetailsToRavenNest();

                this.timeoutHandle = this.kernel.SetTimeout(UpdateTitle, 1000);
            }
            catch
            {
                // Setting title on th is platform probably not supported.
            }
        }

        private async void SendDetailsToRavenNest()
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(stats);
                var statsData = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                using (var www = new HttpClient())
                using (var response = await www.PostAsync("https://www.ravenfall.stream/api/robot/stats", statsData))
                {
                    response.EnsureSuccessStatusCode();
                    detailsDelayTimer = 0;
                }
            }
            catch
            {
                // Unable to send it to RavenNest. Next try should be delayed.
                detailsDelayTimer += 1000;
                if (detailsDelayTimer >= 30_000)
                {
                    detailsDelayTimer = 30_000;
                }
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

            if (sessions.Count > 0)
            {
                stats.UserCount = (UInt32)sessions.Sum(x => x.UserCount);
            }

            return "[Players: " + stats.UserCount + "] ";
        }

        private string GetJoinedChannelCount()
        {
            stats.JoinedChannelsCount = (UInt32)twitch.JoinedChannels().Count;

            return "[Joined: " + stats.JoinedChannelsCount + "] ";
        }

        private string GetSessionsAndConnections()
        {
            stats.ConnectionCount = (UInt32)botServer.AllConnections().Count;
            stats.SessionCount = (UInt32)sessionManager.All().Count;
            return "[Clients: " + stats.SessionCount + "/" + stats.ConnectionCount + "] ";
        }

        private string GetUptime()
        {
            stats.Uptime = DateTime.UtcNow - startedDateTime;
            return "[Uptime: " + FormatTimeSpan(stats.Uptime) + "] ";
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

    public struct StreamBotStats
    {
        public UInt32 JoinedChannelsCount;
        public UInt32 UserCount;
        internal uint ConnectionCount;
        internal uint SessionCount;
        internal TimeSpan Uptime;
    }
}
