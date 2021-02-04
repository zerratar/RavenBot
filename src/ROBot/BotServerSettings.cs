using ROBot.Core.GameServer;

namespace ROBot
{
    public class BotServerSettings : IBotServerSettings
    {
        public string ServerIp { get; set; }

        public int ServerPort { get; set; }
    }
}
