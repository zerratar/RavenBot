using System;
using TwitchLib.Client.Events;

namespace ROBot.Core.Stats
{
    public interface ITwitchStats
    {
        ulong MsgSendCount { get; }
        ulong MsgSentCount { get; }
        ulong TwitchConnectionCurrentAttempt { get; }
        ulong TwitchConnectionCurrentErrorCount { get; }
        ulong TwitchConnectionReconnectCount { get; }
        ulong TwitchConnectionTotalAttempt { get; }
        ulong TwitchConnectionTotalDisconnect { get; }
        ulong TwitchConnectionTotalErrorCount { get; }
        ulong TwitchConnectionTotalSuccess { get; }
        ulong UserChConnectionAttempt { get; }
        ulong UserChConnectionCount { get; }
        ulong UserChConnectionTotalDisconnectCount { get; }
        ulong UserCommandCount { get; }
        ulong UserMsgCount { get; }
        ulong UserTotalCommandCount { get; }
        ulong UserTotalMsgCount { get; }

        void AddMsgRFCmdReceivedCount();
        void AddMsgSend(string channel, string message);
        void AddMsgSent(string channel, string message);
        void AddRFCommandCount();
        void AddTwitchAttempt();
        void AddTwitchDisconnect();
        void AddTwitchError(TwitchLib.Communication.Events.OnErrorEventArgs e);
        void AddTwitchSuccess();
        TimeSpan avgMsgDelays();
        void JoinedChannel(string channel, System.Collections.Generic.IReadOnlyList<TwitchLib.Client.Models.JoinedChannel> joinedChannels);
        void LeftChannel(string channel, System.Collections.Generic.IReadOnlyList<TwitchLib.Client.Models.JoinedChannel> joinedChannels);
        void ReceivedLog(OnLogArgs e);
        void ResetReceivedCount();
        void ResetTwitchAttempt();
        void AddLastRateLimit(OnRateLimitArgs e);
        void AddTwitchError(OnConnectionErrorArgs e);
        void AddChError();
        void AddChAttempt();
    }
}