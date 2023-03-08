﻿using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class SetTimeOfDayRequest
    {
        public SetTimeOfDayRequest(User player, int totalTime, int freezeTime)
        {
            this.Player = player;
            this.TotalTime = totalTime;
            this.FreezeTime = freezeTime;
        }

        public int TotalTime { get; }
        public int FreezeTime { get; }
        public User Player { get; }
    }
}