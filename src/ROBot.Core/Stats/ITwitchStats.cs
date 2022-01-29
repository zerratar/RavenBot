using System;

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

        void AddChDisconnect();
        void AddMsgRFCmdReceivedCount();
        void AddMsgSend(string channel, string message);
        void AddMsgSent(string channel, string message);
        void AddRFCommandCount();
        void AddTwitchAttempt();
        void AddTwitchDisconnect();
        void AddTwitchError();
        void AddTwitchSuccess();
        TimeSpan avgMsgDelays();
        void JoinedChannel(string channel);
        void LeftChannel(string channel);
        void ReceivedLog();
        void ResetReceivedCount();
        void ResetTwitchAttempt();
    }
}