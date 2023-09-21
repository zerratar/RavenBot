using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ROBot.Core;
using ROBot.Core.GameServer;
using ROBot.Core.Stats;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IKernel = Shinobytes.Core.IKernel;
using ROBot.Core.Chat.Discord;
using ROBot.Core.Chat.Twitch;
using Shinobytes.Core;

namespace ROBot
{

    // TODO: https://twitchtokengenerator.com/api/refresh/<refresh_token>
    public class StreamBotApp : IStreamBotApplication
    {
        private readonly IBotStats botStats;
        private readonly ILogger logger;
        private readonly IKernel kernel;
        private readonly IGameSessionManager sessionManager;
        private readonly IBotServer botServer;

        private readonly IDiscordCommandClient discord;
        private readonly ITwitchCommandClient twitch;

        private ITimeoutHandle timeoutHandle;
        private bool disposed;
        private int detailsDelayTimer;
        private bool canUpdateCmdTitle = true;
        private DateTime lastDetailsUpdate;
        private HttpClient httpClient;

        public StreamBotApp(
            ILogger logger,
            IKernel kernel,
            IGameSessionManager sessionManager,
            IBotServer ravenfall,
            ITwitchCommandClient twitch,
            IDiscordCommandClient discord,
            IBotStats botStats)
        {
            this.logger = logger;
            this.kernel = kernel;
            this.sessionManager = sessionManager;
            this.botServer = ravenfall;
            this.twitch = twitch;
            this.discord = discord;
            this.botStats = botStats;


            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true;
            httpClient = new HttpClient(handler);


            //Ravenfall Event
            sessionManager.SessionStarted += OnSessionStarted;
            sessionManager.SessionEnded += OnSessionEnded;
            sessionManager.SessionUpdated += OnSessionUpdated;
        }

        public async Task RunAsync()
        {
            botStats.Started = DateTime.UtcNow;

            logger.LogInformation("[BOT] Application Started");

            logger.LogInformation("[BOT] Starting Bot Server..");
            botServer.Start();

            logger.LogInformation("[BOT] Initializing Twitch Integration..");
            await twitch.StartAsync();

            //logger.LogInformation("[BOT] Initializing Discord Integration..");
            //await discord.StartAsync();

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
                // no need for the bot to spam regarding details update.
                if (DateTime.UtcNow - lastDetailsUpdate < TimeSpan.FromSeconds(2))
                {
                    return;
                }

                if (detailsDelayTimer > 0)
                {
                    await Task.Delay(detailsDelayTimer);
                }

                var json = JsonConvert.SerializeObject(botStats);
                var statsData = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                using (var response = await httpClient.PostAsync("https://www.ravenfall.stream/api/robot/stats", statsData))
                //using (var response = await www.PostAsync("https://localhost:5001/api/robot/stats", statsData))
                {
                    response.EnsureSuccessStatusCode();
                    detailsDelayTimer = 0;
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

                logger.LogError("[BOT] Unable to send Details to RavenNest: " + ex.Message);
            }

            lastDetailsUpdate = DateTime.UtcNow;
        }

        private string GetCommandsPerSecond()
        {
            return "[Total: " + botStats.TotalCommandCount + " C/S: " + botStats.CommandsPerSecondsDelta + " Hi: " + botStats.CommandsPerSecondsMax + "] ";
        }

        private string GetTrackedPlayerCount()
        {
            var sessions = sessionManager.All();

            if (sessions.Count > 0)
            {
                botStats.UserCount = (UInt32)sessions.Sum(x => x.UserCount);
            }

            return "[Players: " + botStats.UserCount + "] ";
        }

        private string GetJoinedChannelCount()
        {
            return "[Joined: " + botStats.JoinedChannelsCount + "] ";
        }

        private string GetSessionsAndConnections()
        {
            botStats.ConnectionCount = (UInt32)botServer.AllConnections().Count;
            botStats.SessionCount = (UInt32)sessionManager.All().Count;
            return "[Clients: " + botStats.SessionCount + "/" + botStats.ConnectionCount + "] ";
        }

        private string GetUptime()
        {
            return "[Uptime: " + FormatTimeSpan(botStats.Uptime) + "] ";
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
            httpClient.Dispose();
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
            discord.EnsureSessionChannel(e.Session);

            if (e.OldName != e.Session.Name)
            {
                //logger.LogWarning("[RVNFLL] Game Session Name Changed (OldName: " + e.OldName + " New: " + e.Session.Name + ")");
                twitch.LeaveChannelAsync(e.OldName);
                twitch.JoinChannelAsync(e.Session.Name);
                return;
            }

            if (!twitch.InChannel(e.Session.Name))
            {
                twitch.JoinChannelAsync(e.Session.Name);
            }
        }

        private void OnSessionStarted(object sender, IGameSession session)
        {
            //logger.LogDebug("[RVNFLL] Game Session Started (Name: " + session.Name + ")");
            twitch.JoinChannelAsync(session.Name);
            botStats.LastSessionStarted = DateTime.UtcNow;
            discord.EnsureSessionChannel(session);
        }

        private void OnSessionEnded(object sender, IGameSession session)
        {
            //logger.LogDebug("[RVNFLL] Game Session Ended (Name: " + session.Name + ")");
            twitch.LeaveChannelAsync(session.Name);
            botStats.LastSessionEnded = DateTime.UtcNow;
            discord.SessionEnded(session);
        }
    }
}
