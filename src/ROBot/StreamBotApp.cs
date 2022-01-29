using Microsoft.Extensions.Logging;
using ROBot.Core;
using ROBot.Core.GameServer;
using ROBot.Core.Twitch;
using ROBot.Ravenfall;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ROBot
{

    // TODO: https://twitchtokengenerator.com/api/refresh/<refresh_token>

    public class StreamBotApp : IStreamBotApplication
    {
        private readonly BotStats stats = new BotStats();
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly Shinobytes.Ravenfall.RavenNet.Core.IKernel kernel;
        private readonly IGameSessionManager sessionManager;
        private readonly IBotServer botServer;
        private readonly ITwitchCommandClient twitch;
        private Shinobytes.Ravenfall.RavenNet.Core.ITimeoutHandle timeoutHandle;
        private bool disposed;
        private DateTime startedDateTime;
        private ulong lastCmdCount;
        private int highestDelta;
        private int detailsDelayTimer;
        private bool canUpdateCmdTitle = true;

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
            stats.Started = this.startedDateTime = DateTime.UtcNow;

            logger.LogInformation("[BOT] Application Started");

            logger.LogInformation("[BOT] Initializing Twitch Integration..");
            twitch.Start();

            logger.LogInformation("[BOT] Starting Bot Server..");
            botServer.Start();

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            var delay = canUpdateCmdTitle ? 1000 : 3000;
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
                if (this.disposed || !canUpdateCmdTitle) return;
                Console.Title = title;
                // Ping RavenNest with details.
            }
            catch
            {
                canUpdateCmdTitle = false;
                // Setting title on th is platform probably not supported.
            }

            try
            {
                SendDetailsToRavenNest();
            }
            catch { }

            this.timeoutHandle = this.kernel.SetTimeout(UpdateTitle, delay);
        }

        private async void SendDetailsToRavenNest()
        {
            try
            {
                if (detailsDelayTimer > 0)
                {
                    await Task.Delay(detailsDelayTimer);
                }

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(stats);
                var statsData = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                statsData.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using (var handler = new HttpClientHandler())
                {
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true;
                    using (var www = new HttpClient(handler))
                    using (var response = await www.PostAsync("https://www.ravenfall.stream/api/robot/stats", statsData))
                    //using (var response = await www.PostAsync("https://localhost:5001/api/robot/stats", statsData))
                    {
                        response.EnsureSuccessStatusCode();
                        detailsDelayTimer = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Unable to send it to RavenNest. Next try should be delayed.
                detailsDelayTimer += 3000;
                if (detailsDelayTimer >= 30_000)
                {
                    detailsDelayTimer = 30_000;
                }

                logger.LogError("[BOT] Unable to send Details to RavenNest: " + ex);
            }
        }

        private string GetCommandsPerSecond()
        {
            ulong commandCount = twitch.GetCommandCount();
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

            stats.TotalCommandCount = (UInt64)commandCount;
            stats.CommandsPerSecondsDelta = delta;
            stats.CommandsPerSecondsMax = (UInt32)highestDelta;

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
            stats.LastSessionStarted = DateTime.UtcNow;
        }

        private void OnSessionEnded(object sender, IGameSession session)
        {
            logger.LogDebug("[RVNFLL] Game Session Ended (Name: " + session.Name + ")");
            twitch.LeaveChannel(session.Name);
            stats.LastSessionEnded = DateTime.UtcNow;
        }
    }

    public class BotStats
    {
        public uint CommandsPerSecondsMax;
        public uint JoinedChannelsCount;
        public uint UserCount;
        public uint ConnectionCount;
        public uint SessionCount;

        public ulong TotalCommandCount;
        public double CommandsPerSecondsDelta;

        public TimeSpan Uptime;
        public DateTime LastSessionStarted;
        public DateTime LastSessionEnded;
        public DateTime Started;
        public DateTime LastUpdated;
    }
}
