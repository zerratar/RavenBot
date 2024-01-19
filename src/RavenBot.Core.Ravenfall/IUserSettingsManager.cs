using RavenBot.Core.Ravenfall.Models;
using System;
using System.Collections.Generic;

namespace RavenBot.Core.Ravenfall
{
    public interface IUserSettingsManager
    {
        UserSettings Get(Guid userId);
        T Get<T>(Guid userId, string key);
        T Get<T>(Guid userId, string key, T defaultValue);
        bool TryGet<T>(Guid userId, string key, out T value);
        void Set<T>(Guid userId, string key, T value);
        void Set(Guid userId, Dictionary<string, object> userSettings);

        UserSettings Get(string platformId, string platform);
        Guid ResolveAccountId(string platformId, string platform);
        IReadOnlyList<UserSettings> GetAll();
    }
}