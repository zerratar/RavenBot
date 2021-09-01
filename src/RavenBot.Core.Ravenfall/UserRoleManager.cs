using RavenBot.Core.Ravenfall.Models;
using System.Collections.Concurrent;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class UserRoleManager : IUserRoleManager
    {
        private const string DefaultRole = "user";
        private const string AdminRole = "admin";
        private const string ModeratorRole = "mod";

        private readonly ConcurrentDictionary<string, string> roles = new ConcurrentDictionary<string, string>();
        private readonly ILogger logger;

        public UserRoleManager(ILogger logger)
        {
            this.logger = logger;
        }

        public string GetRole(string userId)
        {
            if (roles.TryGetValue(userId, out var role))
            {
                return role;
            }
            return DefaultRole;
        }

        public bool IsAdministrator(string userId)
        {
            return GetRole(userId) == AdminRole;
        }

        public bool IsModerator(string userId)
        {
            return GetRole(userId) == ModeratorRole;
        }

        public void SetRole(string userId, string role)
        {
            var oldRole = GetRole(userId);

            if (oldRole != role)
            {
                logger.WriteDebug("User: " + userId + " changed role from " + oldRole + " to " + role);
            }

            roles[userId] = role;
        }
    }
}