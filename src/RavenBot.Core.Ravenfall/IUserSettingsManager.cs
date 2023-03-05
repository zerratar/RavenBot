namespace RavenBot.Core.Ravenfall.Commands
{
    public interface IUserSettingsManager
    {
        UserSettings Get(string userId);
        T Get<T>(string userId, string key);
        T Get<T>(string userId, string key, T defaultValue);
        bool TryGet<T>(string userId, string key, out T value);
        void Set<T>(string userId, string key, T value);
    }
}