using Newtonsoft.Json;
using ROBot.Core.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace ROBot.Core.Stats
{
    public class Stats : ITwitchStats, IBotStats
    {
        //Fields
        //Twitch User Channel Connection Stats
        private ulong _userChConnectionAttempt = 0;
        private System.Collections.Generic.IReadOnlyList<JoinedChannel> _listOfCurrentlyJoinedChannel; //list of joined channels, as reported from twitch

        //Message Sent/Confirmed Stats
        private ulong _msgSendCount = 0; //Bot sent message
        private ulong _msgSentCount = 0; //Bot Successful Message Sent
        private double _oldMsgOffSet = 30; //Time in seconds when to assume message is lost
        private readonly ConcurrentDictionary<string, DateTime> msgTimes = new ConcurrentDictionary<string, DateTime>(); //list of Messages being tracked
        private readonly ConcurrentQueue<TimeSpan> listMsgDelay = new ConcurrentQueue<TimeSpan>(); //list of timespan on time sent to time of confirmed sent

        //Logs and Errors
        private DateTime _lastTwitchLibLog;
        private string _lastTwitchLibLogMessage;
        private DateTime _lastTwitchLibError;
        private string _lastTwitchLibErrorMessage;
        private DateTime _lastRecievedRateLimitedLog;
        private string _lastRecievedRateLimitedMsg;

        //Sessions and Game Connections
        private uint _userCount;
        private uint _connectionCount;
        private uint _sessionCount;
        private DateTime _lastSessionStarted;
        private DateTime _lastSessionEnded;

        //Bot Stats
        private DateTime _started;
        private DateTime _lastUpdated;
        private DateTime _lastCalculate;

        //Commands Stat
        private uint _commandsPerSecondsMax;
        private double _commandsPerSecondsDelta;
        private ulong _userRFCommandCount = 0; //OnCommand Count
        private ulong _userTotalRFCommandCount = 0;
        private ulong _userMsgRFCmdCount = 0; //OnMessage but processed as commands count
        private ulong _userTotalRFMsgCount = 0;
        private ulong _lastRFCmdCount = 0; //last count of Commands before calcuating cmd per seconds

        //Properties 
        //Twitch Connection Stats
        public ulong TwitchConnectionTotalErrorCount { get; private set; }
        public ulong TwitchConnectionTotalAttempt { get; private set; }
        public ulong TwitchConnectionTotalSuccess { get; private set; }
        public ulong TwitchConnectionTotalDisconnect { get; private set; }
        public ulong TwitchConnectionCurrentAttempt { get; private set; }
        public ulong TwitchConnectionCurrentErrorCount { get; private set; }
        public ulong TwitchConnectionReconnectCount { get; private set; }
        public ulong TwitchLibErrorCount { get; private set; }

        //Twitch User Channel Connection Stats
        public ulong UserChConnectionTotalDisconnectCount { get; private set; }
        public uint JoinedChannelsCount => (uint?)ListOfCurrentlyJoinedChannel?.Count ?? 0;
        public ulong UserChConnectionSucesssCount { get; private set; }
        public ulong UserChConnectionDisconnectCount { get; private set; }
        public ulong UserChConnectionTotalSucesssCount { get; private set; }
        public ulong UserChConnectionTotalErrorCount { get; private set; }
        public ulong UserChConnectionErrorCount { get; private set; }
        public ulong UserChConnectionCount { get; private set; }
        public string UserLastChannelJoined { get; private set; }
        public string UserLastChannelLeft { get; private set; }
        public System.Collections.Generic.IReadOnlyList<string> ListOfCurrentlyJoinedChannel { get; set; }

        //Message Sent/Confirmed Stats
        public ulong MsgSendCount { get => _msgSendCount; }
        public ulong MsgSentCount { get => _msgSentCount; }
        public TimeSpan AvgMsgDelay { get => avgMsgDelays(); }

        //Logs and Errors
        public DateTime LastTwitchLibError { get => _lastTwitchLibError; }
        public string LastTwitchLibErrorMessage { get => _lastTwitchLibErrorMessage; }
        public DateTime LastRecievedLog { get => _lastTwitchLibLog; }
        public string LastTwitchLibLogMessage { get => _lastTwitchLibLogMessage; }
        public DateTime LastRecievedRateLimitedLog { get => _lastRecievedRateLimitedLog; }
        public string LastRecievedRateLimitedMsg { get => _lastRecievedRateLimitedMsg; }

        //Sessions and Game Connections
        public uint UserCount { get => _userCount; set => _userCount = value; }
        public uint ConnectionCount { get => _connectionCount; set => _connectionCount = value; }
        public uint SessionCount { get => _sessionCount; set => _sessionCount = value; }
        public DateTime LastSessionStarted { get => _lastSessionStarted; set => _lastSessionStarted = value; }
        public DateTime LastSessionEnded { get => _lastSessionEnded; set => _lastSessionEnded = value; }

        //Bot Stats
        public TimeSpan Uptime { get => DateTime.UtcNow - Started; }
        public DateTime Started { get => _started; set => _started = value; }
        public DateTime LastUpdated { get => _lastUpdated; }

        //Commands Stat
        public ulong UserCommandCount => _userRFCommandCount;
        public ulong UserMsgCount => _userMsgRFCmdCount;
        public ulong UserTotalCommandCount => _userTotalRFCommandCount;
        public ulong UserTotalMsgCount => _userTotalRFMsgCount;
        public ulong UserChConnectionAttempt => _userChConnectionAttempt;
        public uint CommandsPerSecondsMax => Calculate(ref _commandsPerSecondsMax);
        public ulong TotalCommandCount => Calculate(ref _userTotalRFCommandCount);
        public double CommandsPerSecondsDelta => Calculate(ref _commandsPerSecondsDelta);

        //Methods

        private T Calculate<T>(ref T value)
        {
            if (_lastCalculate > DateTime.UtcNow.AddSeconds(1))
                Calculate();

            return value;
        }

        //Couldn't decide when to Calculate values, on when values change(when values is ++)? Seem like it be too often. Upon request of value? would mean needing a check
        //but at least it won't check often. Hmm
        private void Calculate()
        {
            _lastUpdated = _lastCalculate = DateTime.UtcNow;

            double secondsSinceStart = (_lastCalculate - _started).TotalSeconds;
            double delta = _userMsgRFCmdCount - _lastRFCmdCount;
            double csSinceStart = Math.Round(_userMsgRFCmdCount / secondsSinceStart, 2);
            if (delta < csSinceStart)
            {
                delta = csSinceStart;
            }

            if (delta > _commandsPerSecondsMax)
            {
                _commandsPerSecondsMax = (uint)delta;
            }

            _lastRFCmdCount = _userMsgRFCmdCount;
        }

        public void AddMsgRFCmdReceivedCount()
        {
            _lastUpdated = DateTime.UtcNow;
            _userMsgRFCmdCount++;
            _userTotalRFMsgCount++;
        }

        //TODO - Possible Bug - Make new messages sent unique (or confirm to be unique within 30 seconds) otherwise in some cases if the bot
        //sends two messages in the same channel of the same message(spam of !join?)
        public void AddMsgSend(string channel, string message)
        {
            _lastUpdated = DateTime.UtcNow;
            //if we get two of the same message, it'll reject a second (or more), however it'll can make the avg lag at times inaccurate

            msgTimes[GetKey(channel, message)] = DateTime.UtcNow; // Note(zerratar): Access using Indexer will add or replace existing one.
            _msgSendCount++;
            CheckOldMsg();
        }
        public void AddMsgSent(string channel, string message)
        {
            _lastUpdated = DateTime.UtcNow;
            _msgSentCount++;

            var key = GetKey(channel, message);
            if (msgTimes.TryRemove(key, out var value))
            {
                TimeSpan msgDelay = DateTime.UtcNow - value;
                AddMsgDelay(msgDelay);
            }
        }

        public void AddRFCommandCount()
        {
            _lastUpdated = DateTime.UtcNow;
            _userRFCommandCount++;
            _userTotalRFCommandCount++;
        }
        public void AddTwitchAttempt()
        {
            _lastUpdated = DateTime.UtcNow;
            TwitchConnectionCurrentAttempt++;
            TwitchConnectionTotalAttempt++;
        }

        public void AddTwitchDisconnect()
        {
            _lastUpdated = DateTime.UtcNow;
            TwitchConnectionTotalDisconnect++;
        }

        public void AddTwitchError()
        {
            _lastUpdated = DateTime.UtcNow;
            TwitchConnectionTotalErrorCount++;
            TwitchConnectionCurrentErrorCount++;
        }

        public void AddTwitchSuccess()
        {
            _lastUpdated = DateTime.UtcNow;
            TwitchConnectionTotalSuccess++;
        }

        //I suspect that when the bot is runnning well, the avg times is in the sub zero seconds range
        public TimeSpan avgMsgDelays()
        {
            return TimeSpanExtensions.Average(listMsgDelay.AsEnumerable());
        }
        public void JoinedChannel(string channel, System.Collections.Generic.IReadOnlyList<JoinedChannel> joinedChannels)
        {
            _lastUpdated = DateTime.UtcNow;
            UserChConnectionSucesssCount++;
            UserChConnectionTotalSucesssCount++;
            UserLastChannelJoined = channel;

            SetJoinedChannelList(joinedChannels);
        }

        public void LeftChannel(string channel, System.Collections.Generic.IReadOnlyList<JoinedChannel> joinedChannels)
        {
            _lastUpdated = DateTime.UtcNow;
            UserChConnectionTotalDisconnectCount++;
            UserChConnectionDisconnectCount++;
            UserLastChannelLeft = channel;
            SetJoinedChannelList(joinedChannels);
        }

        public void ResetReceivedCount()
        {
            _lastUpdated = DateTime.UtcNow;
            _userMsgRFCmdCount = 0;
            _userRFCommandCount = 0;
            _userChConnectionAttempt = 0;
            UserChConnectionDisconnectCount = 0;
            UserChConnectionSucesssCount = 0;
            UserChConnectionErrorCount = 0;
        }

        public void ResetTwitchAttempt()
        {
            _lastUpdated = DateTime.UtcNow;
            TwitchConnectionCurrentAttempt = 0;
            TwitchConnectionCurrentErrorCount = 0;
        }

        private void AddMsgDelay(TimeSpan msgDelay)
        {
            if (listMsgDelay.Count == 100)
                listMsgDelay.TryDequeue(out _);

            listMsgDelay.Enqueue(msgDelay);
        }

        private void SetJoinedChannelList(IReadOnlyList<JoinedChannel> joinedChannels)
        {
            _listOfCurrentlyJoinedChannel = joinedChannels;
            if (_listOfCurrentlyJoinedChannel == null)
            {
                ListOfCurrentlyJoinedChannel = new List<string>();
            }
            else
            {
                ListOfCurrentlyJoinedChannel = _listOfCurrentlyJoinedChannel.Select(x => x.Channel).ToList();
            }
        }

        private void CheckOldMsg()
        {
            // Note(zerratar): ???? add seconds to current time will always be in the future.
            //                 comparing the msgTime.value will always be false.            

            // DateTime oldMsgTime = DateTime.UtcNow.AddSeconds(_oldMsgOffSet);

            foreach (var msgTime in msgTimes)
            {
                //if (msgTime.Value > oldMsgTime)
                if (DateTime.UtcNow - msgTime.Value >= TimeSpan.FromSeconds(_oldMsgOffSet))
                {
                    msgTimes.TryRemove(msgTime.Key, out _);
                    AddMsgDelay(TimeSpan.FromSeconds(30)); //May be inaccuate due to possible non-unique messages
                }

            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetKey(string channel, string message)
        {
            if (!string.IsNullOrEmpty(channel))
                channel = channel.ToLower().Trim();
            if (!string.IsNullOrEmpty(message))
                message = message.ToLower().Trim();
            return channel + message;
        }

        public void AddTwitchError(OnErrorEventArgs e)
        {
            _lastTwitchLibError = _lastUpdated = DateTime.UtcNow;
            _lastTwitchLibErrorMessage = JsonConvert.SerializeObject(e);
        }

        //public void ReceivedLog(OnLogArgs e)
        //{
        //    _lastTwitchLibLog = _lastUpdated = DateTime.UtcNow;
        //    _lastTwitchLibLogMessage = JsonConvert.SerializeObject(e);
        //}

        public void AddLastRateLimit(OnRateLimitArgs e)
        {
            _lastRecievedRateLimitedLog = _lastUpdated = DateTime.UtcNow;
            _lastRecievedRateLimitedMsg = JsonConvert.SerializeObject(e);
        }

        public void AddTwitchError(OnConnectionErrorArgs e)
        {
            _lastTwitchLibError = _lastUpdated = DateTime.UtcNow;
            _lastTwitchLibErrorMessage = JsonConvert.SerializeObject(e);
        }

        public void AddChError()
        {
            _lastUpdated = DateTime.UtcNow;
            UserChConnectionErrorCount++;
            UserChConnectionTotalErrorCount++;
        }

        public void AddChAttempt()
        {
            _lastUpdated = DateTime.UtcNow;
            _userChConnectionAttempt++;
        }
    }
}