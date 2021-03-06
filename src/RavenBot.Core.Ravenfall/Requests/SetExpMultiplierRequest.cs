﻿using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class SetExpMultiplierRequest
    {
        public SetExpMultiplierRequest(Player player, int expMultiplier)
        {
            this.Player = player;
            this.ExpMultiplier = expMultiplier;
        }

        public int ExpMultiplier { get; }
        public Player Player { get; }
    }
}