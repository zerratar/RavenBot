using RavenBot.Core.Net;

namespace ROBot.Core
{
    public class SessionGameMessageResponse
    {
        public SessionGameMessageResponse(
            IGameSession session,
            GameMessageResponse message)
        {
            this.Session = session;
            this.Message = message;
        }

        public IGameSession Session { get; }
        public GameMessageResponse Message { get; }
    }
}
