using Microsoft.Extensions.Logging;
using ROBot.Core;
using ROBot.Core.GameServer;
using ROBot.Core.Twitch;
using ROBot.Ravenfall;
namespace ROBot
{
    public class StreamBotApp : IStreamBotApplication
    {
        private readonly ILogger logger;
        private readonly IGameSessionManager sessionManager;
        private readonly IBotServer botServer;
        private readonly ITwitchCommandClient twitch;
        private bool disposed;

        public StreamBotApp(
            ILogger logger,
            IGameSessionManager sessionManager,
            IBotServer ravenfall,
            ITwitchCommandClient twitch)
        {
            this.logger = logger;
            this.sessionManager = sessionManager;
            this.botServer = ravenfall;
            this.twitch = twitch;

            sessionManager.SessionStarted += OnSessionStarted;
            sessionManager.SessionEnded += OnSessionEnded;
            sessionManager.SessionUpdated += OnSessionUpdated;
        }


        public void Run()
        {
            logger.LogInformation("Application Started");

            logger.LogInformation("Initializing Twitch Integration..");
            twitch.Start();

            logger.LogInformation("Starting Bot Server..");
            botServer.Start();
        }

        public void Shutdown()
        {
            logger.LogInformation("Application Shutdown initialized.");
            Dispose();
        }

        public void Dispose()
        {
            if (disposed) return;
            sessionManager.SessionStarted -= OnSessionStarted;
            sessionManager.SessionEnded -= OnSessionEnded;
            sessionManager.SessionUpdated -= OnSessionUpdated;
            disposed = true;
            twitch.Dispose();
            botServer.Dispose();
        }

        private void OnSessionUpdated(object sender, GameSessionUpdateEventArgs e)
        {
            twitch.LeaveChannel(e.OldName);
            twitch.JoinChannel(e.NewName);
        }

        private void OnSessionStarted(object sender, IGameSession session)
        {
            twitch.JoinChannel(session.Name);
        }

        private void OnSessionEnded(object sender, IGameSession session)
        {
            twitch.LeaveChannel(session.Name);
        }
    }
}
