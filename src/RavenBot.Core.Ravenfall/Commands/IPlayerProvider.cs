using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{
    public interface IPlayerProvider
    {
        Player Get(string userId, string username);
        Player Get(ICommandSender sender, string identifier = null);
        Player Get(string username);
    }

    public class PlayerProvider : IPlayerProvider
    {
        public Player Get(ICommandSender sender, string identifier = null)
        {
            if (string.IsNullOrEmpty(identifier?.Trim()))
                identifier = "1";

            return new Player(
                sender.UserId,
                sender.Username,
                sender.DisplayName,
                sender.ColorHex,
                sender.IsBroadcaster,
                sender.IsModerator,
                sender.IsSubscriber,
                sender.IsVip,
                identifier);
        }

        public Player Get(string username)
        {
            return new Player(null, username, username, null, false, false, false, false, "1");
        }

        public Player Get(string userId, string username)
        {
            return new Player(userId, username, username, null, false, false, false, false, "1");
        }
    }
}