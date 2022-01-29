using System;

namespace ROBot
{
    public interface IBotStats
    {
        double CommandsPerSecondsDelta { get; set; }
        uint CommandsPerSecondsMax { get; set; }
        uint ConnectionCount { get; set; }
        uint JoinedChannelsCount { get; set; }
        DateTime LastSessionEnded { get; set; }
        DateTime LastSessionStarted { get; set; }
        DateTime LastTwitchLibError { get; set; }
        string LastTwitchLibErrorMessage { get; set; }
        string LastTwitchLibLogMessage { get; set; }
        DateTime LastUpdated { get; set; }
        uint SessionCount { get; set; }
        DateTime Started { get; set; }
        ulong TotalCommandCount { get; set; }
        ulong TwitchLibErrorCount { get; set; }
        TimeSpan Uptime { get; set; }
        uint UserCount { get; set; }
    }
}