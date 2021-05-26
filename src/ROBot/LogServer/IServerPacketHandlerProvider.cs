namespace ROBot
{
    public interface IServerPacketHandlerProvider
    {
        IServerPacketHandler Get(string type);
    }
}