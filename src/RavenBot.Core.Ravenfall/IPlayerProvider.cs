using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{
    public interface IPlayerProvider
    {
        int Count { get; }
        Player Get(System.Guid userId);
        Player Get(string userId, string username, string platform = "twitch");
        Player Get(ICommandSender sender, string identifier = null);
        Player Get(string username, string platform = "twitch");
        Player GetByUserId(string twitchUserId, string platform = "twitch");
        Player GetBroadcaster();
    }
}