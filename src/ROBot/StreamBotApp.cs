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
using RavenBot.Core.Chat;
using System.Collections.Concurrent;

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
        private ITimeoutHandle queueTimeoutHandle;

        private readonly ConcurrentQueue<UserSubscriptionEvent> subQueue = new ConcurrentQueue<UserSubscriptionEvent>();
        private readonly ConcurrentQueue<CheerBitsEvent> cheerBitsQueue = new ConcurrentQueue<CheerBitsEvent>();

        public StreamBotApp(
            ILogger logger,
            IKernel kernel,
            IMessageBus messageBus,
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

            messageBus.Subscribe<CheerBitsEvent>(nameof(CheerBitsEvent), OnUserCheer);
            messageBus.Subscribe<UserSubscriptionEvent>(nameof(UserSubscriptionEvent), OnUserSub);

            this.queueTimeoutHandle = kernel.SetTimeout(HandleTwitchEventQueue, 30000);
        }

        private async void HandleTwitchEventQueue()
        {
            try
            {
                while (cheerBitsQueue.TryDequeue(out var evt))
                {
                    if (!await OnUserCheerImplAsync(evt, false))
                    {
                        cheerBitsQueue.Enqueue(evt);
                        break; // try again later
                    }
                }
            }
            catch { }

            try
            {
                while (subQueue.TryDequeue(out var evt))
                {
                    if (!await OnUserSubImplAsync(evt, false))
                    {
                        subQueue.Enqueue(evt);
                        break; // try again later
                    }
                }
            }
            catch { }

            this.queueTimeoutHandle = kernel.SetTimeout(HandleTwitchEventQueue, 30000);
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

        private async void OnUserSub(UserSubscriptionEvent @event)
        {
            await OnUserSubImplAsync(@event, true);
        }

        private async void OnUserCheer(CheerBitsEvent @event)
        {
            await OnUserCheerImplAsync(@event, true);
        }

        private async Task<bool> OnUserCheerImplAsync(CheerBitsEvent @event, bool addToQueueOnFailure)
        {
            // TODO: implement
            // 1. try send to raven nest
            // 2. if failed, add to a retry queue with the same object
            // return false if failed, true if success
            try
            {
                var json = JsonConvert.SerializeObject(@event);
                var statsData = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
#if DEBUG
                using (var response = await httpClient.PostAsync("https://localhost:5001/api/robot/twitch-cheer", statsData))
#else
                using (var response = await httpClient.PostAsync("https://www.ravenfall.stream/api/robot/twitch-cheer", statsData))
#endif
                {
                    response.EnsureSuccessStatusCode();
                }

                return true;
            }
            catch
            {
                if (addToQueueOnFailure)
                {
                    cheerBitsQueue.Enqueue(@event);
                }
                return false;
            }
        }

        private async Task<bool> OnUserSubImplAsync(UserSubscriptionEvent @event, bool addToQueueOnFailure)
        {
            // TODO: implement
            // 1. try send to raven nest
            // 2. if failed, add to a retry queue with the same object
            // return false if failed, true if success
            try
            {
                var json = JsonConvert.SerializeObject(@event);
                var statsData = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
#if DEBUG
                using (var response = await httpClient.PostAsync("https://localhost:5001/api/robot/twitch-sub", statsData))
#else
                using (var response = await httpClient.PostAsync("https://www.ravenfall.stream/api/robot/twitch-sub", statsData))
#endif
                {
                    response.EnsureSuccessStatusCode();
                }

                return true;
            }
            catch
            {
                if (addToQueueOnFailure)
                {
                    subQueue.Enqueue(@event);
                }
                return false;
            }
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
#if DEBUG
                using (var response = await httpClient.PostAsync("https://localhost:5001/api/robot/stats", statsData))
#else
                using (var response = await httpClient.PostAsync("https://www.ravenfall.stream/api/robot/stats", statsData))
#endif
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
            return "[Connections: " + botStats.ConnectionCount + "] [Sessions: " + botStats.SessionCount + "] ";
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

        private async void OnSessionUpdated(object sender, GameSessionUpdateEventArgs e)
        {
            discord.EnsureSessionChannel(e.Session);

            if (e.OldName != e.Session.Name)
            {
                //logger.LogWarning("[RVNFLL] Game Session Name Changed (OldName: " + e.OldName + " New: " + e.Session.Name + ")");
                await twitch.LeaveChannelAsync(e.OldName);
                await twitch.JoinChannelAsync(e.Session.Name);
                return;
            }

            if (!twitch.InChannel(e.Session.Name))
            {
                await twitch.JoinChannelAsync(e.Session.Name);
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
