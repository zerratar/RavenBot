using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System.Linq;

namespace RavenBot.Core.Ravenfall.Commands
{
    public interface IPlayerProvider
    {
        Player Get(string userId, string username);
        Player Get(ICommandSender sender, string identifier = null);
        Player Get(string username);
        Player GetBroadcaster();
    }

    public class PlayerProvider : IPlayerProvider
    {

        private readonly System.Collections.Generic.List<Player> createdPlayers = new System.Collections.Generic.List<Player>();

        public Player Get(ICommandSender sender, string identifier = null)
        {
            if (string.IsNullOrEmpty(identifier?.Trim()))
                identifier = "1";

            var player = createdPlayers.FirstOrDefault(x => x.UserId == sender.UserId && x.Identifier == identifier);
            if (player == null)
            {
                player = new Player();
                createdPlayers.Add(player);
            }

            player.UserId = sender.UserId;
            player.Username = sender.Username;
            player.DisplayName = sender.DisplayName;
            player.Color = sender.ColorHex;
            player.IsBroadcaster = sender.IsBroadcaster;
            player.IsModerator = sender.IsModerator;
            player.IsSubscriber = sender.IsSubscriber;
            player.IsVip = sender.IsVip;
            player.Identifier = identifier;

            return player;
        }

        public Player Get(string username)
        {
            var plr = createdPlayers.FirstOrDefault(x => x.Username == username);
            if (plr != null) return plr;
            plr = new Player(null, username, username, null, false, false, false, false, "1");
            createdPlayers.Add(plr);
            return plr;
        }

        public Player Get(string userId, string username)
        {
            var plr = createdPlayers.FirstOrDefault(x => x.Username == username);
            if (plr != null)
            {
                plr.UserId = userId;
                return plr;
            }
            plr = new Player(userId, username, username, null, false, false, false, false, "1");
            createdPlayers.Add(plr);
            return plr;
        }

        public Player GetBroadcaster()
        {
            return createdPlayers.FirstOrDefault(x => x.IsBroadcaster);
        }
    }
}