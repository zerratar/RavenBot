namespace ROBot.Core.GameServer
{
    public interface IBotServerSettings
    {
        string ServerIp { get; }
        int ServerPort { get; }
    }
}
