using Newtonsoft.Json;
using ROBot.Core.Extensions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace ROBot.Core.Stats
{
    public class Stats : ITwitchStats, IBotStats
    {
        //Fields
        //Twitch Connection Stats
        private ulong _twitchConnectionTotalErrorCount = 0;
        private ulong _twitchConnectionTotalAttempt = 0;
        private ulong _twitchConnectionTotalSuccess = 0;
        private ulong _twitchConnectionTotalDisconnect = 0;
        private ulong _twitchConnectionCurrentAttempt = 0;
        private ulong _twitchConnectionCurrentErrorCount = 0;
        private ulong _twitchConnectionReconnectCount = 0;

        //Twitch User Channel Connection Stats
        private ulong _userChConnectionErrorCount = 0;
        private ulong _userChConnectionTotalErrorCount = 0;
        private ulong _userChConnectionDisconnectCount = 0;
        private ulong _userChConnectionTotalDisconnectCount = 0;
        private ulong _userChConnectionSucesssCount = 0;
        private ulong _userChConnectionTotalSucesssCount = 0;
        private ulong _userChConnectionAttempt = 0;
        private string _userLastChannelJoined;
        private string _userLastChannelLeft;
        private System.Collections.Generic.IReadOnlyList<JoinedChannel> _listOfCurrentlyJoinedChannel; //list of joined channels, as reported from twitch

        //Message Sent/Confirmed Stats
        private ulong _msgSendCount = 0; //Bot sent message
        private ulong _msgSentCount = 0; //Bot Successful Message Sent
        private double _oldMsgOffSet = 30; //Time in seconds when to assume message is lost
        private ConcurrentDictionary<object, DateTime> msgTimes = new ConcurrentDictionary<object, DateTime>(); //list of Messages being tracked
        private ConcurrentQueue<TimeSpan> listMsgDelay = new ConcurrentQueue<TimeSpan>(); //list of timespan on time sent to time of confirmed sent
        
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
        public ulong TwitchConnectionTotalErrorCount { get => _twitchConnectionTotalErrorCount; }
        public ulong TwitchConnectionTotalAttempt { get => _twitchConnectionTotalAttempt; }
        public ulong TwitchConnectionTotalSuccess { get => _twitchConnectionTotalSuccess; }
        public ulong TwitchConnectionTotalDisconnect { get => _twitchConnectionTotalDisconnect; }
        public ulong TwitchConnectionCurrentAttempt { get => _twitchConnectionCurrentAttempt; }
        public ulong TwitchConnectionCurrentErrorCount { get => _twitchConnectionCurrentErrorCount; }
        public ulong TwitchConnectionReconnectCount { get => _twitchConnectionReconnectCount; }
        public ulong TwitchLibErrorCount { get => _twitchConnectionCurrentErrorCount; }

        //Twitch User Channel Connection Stats
        public ulong UserChConnectionTotalDisconnectCount { get => _userChConnectionTotalDisconnectCount; }
        public uint JoinedChannelsCount { get => (uint?)_listOfCurrentlyJoinedChannel?.Count ?? 0; }
        public ulong UserChConnectionSucesssCount { get => _userChConnectionSucesssCount; }
        public ulong UserChConnectionTotalSucesssCount { get => _userChConnectionTotalSucesssCount; }
        public ulong UserChConnectionTotalErrorCount { get => _userChConnectionTotalErrorCount; }
        public ulong UserChConnectionErrorCount { get => _userChConnectionErrorCount; }
        public ulong UserChConnectionCount { get => _userChConnectionDisconnectCount; }
        public string UserLastChannelJoined { get => _userLastChannelJoined; }
        public string UserLastChannelLeft { get => _userLastChannelLeft; }
        public System.Collections.Generic.IReadOnlyList<JoinedChannel> ListOfCurrentlyJoinedChannel { get => _listOfCurrentlyJoinedChannel; }

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
        public ulong UserCommandCount { get => _userRFCommandCount; }
        public ulong UserMsgCount { get => _userMsgRFCmdCount; }
        public ulong UserTotalCommandCount { get => _userTotalRFCommandCount; }
        public ulong UserTotalMsgCount { get => _userTotalRFMsgCount; }
        public ulong UserChConnectionAttempt { get => _userChConnectionAttempt; }
        public uint CommandsPerSecondsMax { get => getCommandPerSecondMax(); }
        public ulong TotalCommandCount { get => getTotalCommandCount(); }
        public double CommandsPerSecondsDelta { get => getCommandPerSecondDelta(); }
        
        //Methods

        private ulong getTotalCommandCount()
        {
            if (_lastCalculate > DateTime.UtcNow.AddSeconds(1))
                Calculate();
            return _userTotalRFCommandCount;
        }
        private double getCommandPerSecondDelta()
        {
            if (_lastCalculate > DateTime.UtcNow.AddSeconds(1))
                Calculate();
            return _commandsPerSecondsDelta;
        }
        private uint getCommandPerSecondMax()
        {
            if (_lastCalculate > DateTime.UtcNow.AddSeconds(1))
                Calculate();
            return _commandsPerSecondsMax;
        }

        //Couldn't decide when to Calculate values, on when values change(when values is ++)? Seem like it be too often. Upon request of value? would mean needing a check
        //but at least it won't check often. Hmm
        private void Calculate()
        {
            _lastUpdated =_lastCalculate = DateTime.UtcNow;

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
            msgTimes.TryAdd(GetObject(channel, message), DateTime.UtcNow); 
            _msgSendCount++;
            CheckOldMsg();
        }
        public void AddMsgSent(string channel, string message)
        {
            _lastUpdated = DateTime.UtcNow;
            _msgSentCount++;
            object thisObj = GetObject(channel, message);
            DateTime value;
            if (msgTimes.TryGetValue(thisObj, out value))
            {
                TimeSpan msgDelay = DateTime.UtcNow - value;
                AddMsgDelay(msgDelay);
                msgTimes.TryRemove(thisObj, out _);
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
            _twitchConnectionCurrentAttempt++;
            _twitchConnectionTotalAttempt++;
        }

        public void AddTwitchDisconnect()
        {
            _lastUpdated = DateTime.UtcNow;
            _twitchConnectionTotalDisconnect++;
        }

        public void AddTwitchError()
        {
            _lastUpdated = DateTime.UtcNow;
            _twitchConnectionTotalErrorCount++;
            _twitchConnectionCurrentErrorCount++;
        }

        public void AddTwitchSuccess()
        {
            _lastUpdated = DateTime.UtcNow;
            _twitchConnectionTotalSuccess++;
        }

        //I suspect that when the bot is runnning well, the avg times is in the sub zero seconds range
        public TimeSpan avgMsgDelays()
        {
            return TimeSpanExtensions.Average(listMsgDelay.AsEnumerable());
        }
        public void JoinedChannel(string channel, System.Collections.Generic.IReadOnlyList<JoinedChannel> joinedChannels)
        {
            _lastUpdated = DateTime.UtcNow;
            _userChConnectionSucesssCount++;
            _userChConnectionTotalSucesssCount++;
            _userLastChannelJoined = channel;
            _listOfCurrentlyJoinedChannel = joinedChannels;
        }

        public void LeftChannel(string channel, System.Collections.Generic.IReadOnlyList<JoinedChannel> joinedChannels)
        {
            _lastUpdated = DateTime.UtcNow;
            _userChConnectionTotalDisconnectCount++;
            _userChConnectionDisconnectCount++;
            _userLastChannelLeft = channel;
            _listOfCurrentlyJoinedChannel = joinedChannels;
        }

        public void ResetReceivedCount()
        {
            _lastUpdated = DateTime.UtcNow;
            _userMsgRFCmdCount = 0;
            _userRFCommandCount = 0;
            _userChConnectionAttempt = 0;
            _userChConnectionDisconnectCount = 0;
            _userChConnectionSucesssCount = 0;
            _userChConnectionErrorCount = 0;  
        }

        public void ResetTwitchAttempt()
        {
            _lastUpdated = DateTime.UtcNow;
            _twitchConnectionCurrentAttempt = 0;
            _twitchConnectionCurrentErrorCount = 0;
        }

        private void AddMsgDelay(TimeSpan msgDelay)
        {
            if (listMsgDelay.Count == 100)
                listMsgDelay.TryDequeue(out _);

            listMsgDelay.Enqueue(msgDelay);
        }

        private void CheckOldMsg()
        {
            DateTime oldMsgTime = DateTime.UtcNow.AddSeconds(_oldMsgOffSet);

            foreach (var msgTime in msgTimes)
            {
                if (msgTime.Value > oldMsgTime)
                {
                    msgTimes.TryRemove(msgTime.Key, out _);
                    AddMsgDelay(TimeSpan.FromSeconds(30)); //May be inaccuate due to possible non-unique messages
                }
                    
            }
        }

        private object GetObject(string channel, string message)
        {
            channel = channel.ToLower().Trim();
            message = message.ToLower().Trim();

            return channel + message;
        }

        public void AddTwitchError(OnErrorEventArgs e)
        {
            _lastTwitchLibError = _lastUpdated = DateTime.UtcNow;
            _lastTwitchLibErrorMessage = JsonConvert.SerializeObject(e);
        }

        public void ReceivedLog(OnLogArgs e)
        {
            _lastTwitchLibLog = _lastUpdated = DateTime.UtcNow;
            _lastTwitchLibLogMessage = JsonConvert.SerializeObject(e);
        }

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
            _userChConnectionErrorCount++;
            _userChConnectionTotalErrorCount++;
        }

        public void AddChAttempt()
        {
            _lastUpdated = DateTime.UtcNow;
            _userChConnectionAttempt++;
        }
    }
}