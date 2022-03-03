using System;

namespace ROBot.Core.Stats
{
    public interface IBotStats
    {
        double CommandsPerSecondsDelta { get; }
        uint CommandsPerSecondsMax { get; }
        uint ConnectionCount { get; set; }
        uint JoinedChannelsCount { get; }
        DateTime LastSessionEnded { get; set; }
        DateTime LastSessionStarted { get; set; }
        DateTime LastTwitchLibError { get; }
        string LastTwitchLibErrorMessage { get; }
        DateTime LastRecievedLog { get; }
        string LastTwitchLibLogMessage { get; }
        DateTime LastRecievedRateLimitedLog { get; }
        string LastRecievedRateLimitedMsg { get; }
        DateTime LastUpdated { get; }
        uint SessionCount { get; set; }
        DateTime Started { get; set; }
        ulong TotalCommandCount { get; }
        ulong TwitchLibErrorCount { get; }
        TimeSpan Uptime { get; }
        uint UserCount { get; set; }
        System.Collections.Generic.IReadOnlyList<TwitchLib.Client.Models.JoinedChannel> ListOfCurrentlyJoinedChannel { get; }
        string UserLastChannelJoined { get; }
        string UserLastChannelLeft { get; }
        TimeSpan AvgMsgDelay { get; }

    }
}