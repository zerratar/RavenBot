using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{
    public interface IUserRoleManager
    {
        bool IsAdministrator(string userId);
        bool IsModerator(string userId);
        void SetRole(string userId, string role);
        string GetRole(string userId);
    }
}