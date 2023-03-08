﻿using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerStatsRequest
    {
        public User Player { get; }
        public string Skill { get; }

        public PlayerStatsRequest(User player, string skill)
        {
            Player = player;
            Skill = skill;
        }
    }
}