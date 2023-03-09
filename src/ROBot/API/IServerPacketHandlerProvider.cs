namespace ROBot.API
{
    public interface IServerPacketHandlerProvider
    {
        IServerPacketHandler Get(string type);
    }
}