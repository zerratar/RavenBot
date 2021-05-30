using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System;
using System.Linq;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class PlayerProvider : IPlayerProvider
    {

        private readonly System.Collections.Generic.List<Player> createdPlayers = new System.Collections.Generic.List<Player>();

        private readonly object mutex = new object();
        public int Count
        {
            get
            {
                lock (mutex)
                {
                    return createdPlayers.Count;
                }
            }
        }

        public Player Get(ICommandSender sender, string identifier = null)
        {
            lock (mutex)
            {
                if (string.IsNullOrEmpty(identifier?.Trim()))
                    identifier = "1";

                var player = createdPlayers.FirstOrDefault(x => x.Username == sender.Username || x.UserId == sender.UserId && x.Identifier == identifier);
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
        }

        public Player Get(string username)
        {

            lock (mutex)
            {
                var plr = createdPlayers.FirstOrDefault(x => x.Username == username);
                if (plr != null) return plr;
                plr = new Player(null, username, username, null, false, false, false, false, "1");
                createdPlayers.Add(plr);
                return plr;
            }
        }

        public Player Get(string userId, string username)
        {
            lock (mutex)
            {
                var plr = createdPlayers.FirstOrDefault(x => x.Username == username || x.UserId == userId);
                if (plr != null)
                {
                    plr.UserId = userId;
                    plr.DisplayName = username;
                    return plr;
                }
                plr = new Player(userId, username, username, null, false, false, false, false, "1");
                createdPlayers.Add(plr);
                return plr;
            }
        }

        public bool Contains(Player player)
        {
            lock (mutex)
            {
                if (createdPlayers.Contains(player))
                    return true;

                return GetById(player.UserId) != null; ;
            }
        }

        public Player GetById(string userId)
        {
            lock (mutex)
            {
                return createdPlayers.FirstOrDefault(x => x.UserId == userId);
            }
        }

        public bool RemoveById(string twitchId)
        {
            lock (mutex)
            {
                var user = GetById(twitchId);
                if (user == null) return false;
                return createdPlayers.Remove(user);
            }
        }

        public Player GetBroadcaster()
        {
            lock (mutex)
            {
                return createdPlayers.FirstOrDefault(x => x.IsBroadcaster);
            }
        }
    }
}