using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public interface IUserProvider
    {
        int Count { get; }
        User Get(ICommand cmd);
        User Get(System.Guid userId);
        User Get(string userId, string username, string platform = "twitch");
        User Get(ICommandSender sender, string identifier = null);
        User Get(string username, string platform = "twitch");
        User GetByUserId(string platformId, string platform = "twitch");
        User GetBroadcaster();
        void SetBroadcaster(string platformId, string username, string platform = "twitch");
    }
}