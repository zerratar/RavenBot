using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{
    public interface IPlayerProvider
    {
        Player Get(ICommandSender sender);
        Player Get(string username);
    }

    public class PlayerProvider : IPlayerProvider
    {
        public Player Get(ICommandSender sender)
        {
            return new Player(
                sender.UserId,
                sender.Username,
                sender.DisplayName,
                sender.ColorHex,
                sender.IsBroadcaster,
                sender.IsModerator,
                sender.IsSubscriber);
        }

        public Player Get(string username)
        {
            return new Player(null, username, username, null, false, false, false);
        }
    }
}