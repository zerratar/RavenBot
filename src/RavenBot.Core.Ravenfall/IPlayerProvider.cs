using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System.Linq;

namespace RavenBot.Core.Ravenfall.Commands
{
    public interface IPlayerProvider
    {
        int Count { get; }
        Player Get(string userId, string username);
        Player Get(ICommandSender sender, string identifier = null);
        Player Get(string username);
        Player GetBroadcaster();
    }
}