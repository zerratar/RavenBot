using Newtonsoft.Json;
using ROBot.Core.Extensions;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ROBot.Core.Stats
{
    public class Stats : ITwitchStats, IBotStats
    {
        //Fields
        //set alert threshold
        //connectionGap
        //msgGap
        //avg connection time
        //avg msg times
        //

        //Twitch Connection Stats
        private ulong _twitchConnectionTotalErrorCount = 0;
        private ulong _twitchConnectionTotalAttempt = 0;
        private ulong _twitchConnectionTotalSuccess = 0;
        private ulong _twitchConnectionTotalDisconnect = 0;
        private ulong _twitchConnectionCurrentAttempt = 0;
        private ulong _twitchConnectionCurrentErrorCount = 0;
        private ulong _twitchConnectionReconnectCount = 0;

        //User Channel Connection Stats
        private ulong _userChConnectionTotalCount = 0;
        private ulong _userChConnectionTotalDisconnectCount = 0;
        private ulong _userChConnectionAttempt = 0;

        //Message Sent Stats
        private ulong msgSendCount = 0;
        private ulong msgSentCount = 0;
        private ConcurrentDictionary<object, DateTime> msgTimes = new ConcurrentDictionary<object, DateTime>();
        private ConcurrentQueue<TimeSpan> listMsgDelay = new ConcurrentQueue<TimeSpan>();

        //Recieved from User Stats
        private ulong _userRFCommandCount = 0;
        private ulong _userMsgRFCmdCount = 0;

        private ulong _userTotalRFCommandCount = 0;
        private ulong _userTotalRFMsgCount = 0;

        //Recieved Logs Stats (onLogs = event raised everytime TwitchLib.Clients logs something. Usually data from twitch.)
        private DateTime lastRecievedLog;
        private object msgTimesMutex;
        private uint _commandsPerSecondsMax;
        private uint _joinedChannelsCount;
        private uint _userCount;
        private uint _connectionCount;
        private uint _sessionCount;
        private ulong _totalCommandCount;
        private double commandsPerSecondsDelta;
        private string _lastTwitchLibErrorMessage;
        private DateTime _lastTwitchLibError;
        private string _lastTwitchLibLogMessage;
        private TimeSpan _uptime;
        private DateTime _lastSessionStarted;
        private DateTime _lastSessionEnded;
        private DateTime _started;
        private DateTime _lastUpdated;

        //private void Twitch_OnTwitchLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        //{
        //    try
        //    {
        //        botStats.LastTwitchLibLogMessage = JsonConvert.SerializeObject(e);
        //    }
        //    catch { }
        //}

        //private void Twitch_OnTwitchError(object sender, TwitchLib.Communication.Events.OnErrorEventArgs e)
        //{
        //    try
        //    {
        //        botStats.TwitchLibErrorCount++;
        //        botStats.LastTwitchLibErrorMessage = e.Exception.ToString();
        //        botStats.LastTwitchLibError = DateTime.UtcNow;
        //    }
        //    catch { }
        //}


        //sortedList of top 10 idle times
        //last 20 idleTimes

        //use events to alert for unusnual twitch stats, such as higher than normal avg times between sent messages and sent confirmation. 

        //Properties 
        public ulong UserCommandCount { get => _userRFCommandCount; }
        public ulong UserMsgCount { get => _userMsgRFCmdCount; }
        public ulong UserTotalCommandCount { get => _userTotalRFCommandCount; }
        public ulong UserTotalMsgCount { get => _userTotalRFMsgCount; }
        public ulong TwitchConnectionTotalErrorCount { get => _twitchConnectionTotalErrorCount; }
        public ulong TwitchConnectionTotalAttempt { get => _twitchConnectionTotalAttempt; }
        public ulong TwitchConnectionTotalSuccess { get => _twitchConnectionTotalSuccess; }
        public ulong TwitchConnectionTotalDisconnect { get => _twitchConnectionTotalDisconnect; }
        public ulong TwitchConnectionCurrentAttempt { get => _twitchConnectionCurrentAttempt; }
        public ulong TwitchConnectionCurrentErrorCount { get => _twitchConnectionCurrentErrorCount; }
        public ulong TwitchConnectionReconnectCount { get => _twitchConnectionReconnectCount; }
        public ulong UserChConnectionCount { get => _userChConnectionTotalCount; }
        public ulong UserChConnectionAttempt { get => _userChConnectionAttempt; }
        public ulong MsgSendCount { get => msgSendCount; }
        public ulong MsgSentCount { get => msgSentCount; }
        public ulong UserChConnectionTotalDisconnectCount { get => _userChConnectionTotalDisconnectCount; }

        public uint CommandsPerSecondsMax { get => _commandsPerSecondsMax; set => _commandsPerSecondsMax = value; }
        public uint JoinedChannelsCount { get => _joinedChannelsCount; set => _joinedChannelsCount = value; }
        public uint UserCount { get => _userCount; set => _userCount = value; }
        public uint ConnectionCount { get => _connectionCount; set => _connectionCount = value; }
        public uint SessionCount { get => _sessionCount; set => _sessionCount = value; }
        public ulong TotalCommandCount { get => _totalCommandCount; set => _totalCommandCount = value; }
        public double CommandsPerSecondsDelta { get => commandsPerSecondsDelta; set => commandsPerSecondsDelta = value; }
        public ulong TwitchLibErrorCount { get => _twitchConnectionCurrentErrorCount; set => _twitchConnectionCurrentErrorCount = value; }
        public string LastTwitchLibErrorMessage { get => _lastTwitchLibErrorMessage; set => _lastTwitchLibErrorMessage = value; }
        public DateTime LastTwitchLibError { get => _lastTwitchLibError; set => _lastTwitchLibError = value; }
        public string LastTwitchLibLogMessage { get => _lastTwitchLibLogMessage; set => _lastTwitchLibLogMessage = value; }
        public TimeSpan Uptime { get => _uptime; set => _uptime = value; }
        public DateTime LastSessionStarted { get => _lastSessionStarted; set => _lastSessionStarted = value; }
        public DateTime LastSessionEnded { get => _lastSessionEnded; set => _lastSessionEnded = value; }
        public DateTime Started { get => _started; set => _started = value; }
        public DateTime LastUpdated { get => _lastUpdated; set => _lastUpdated = value; }

        public void AddChDisconnect()
        {
            //throw new NotImplementedException();
        }

        public void AddMsgRFCmdReceivedCount()
        {
            this._userMsgRFCmdCount++;
            this._userTotalRFMsgCount++;
        }

        public void AddMsgSend(string channel, string message)
        {
            msgTimes.TryAdd(GetObject(channel, message), DateTime.Now);
        }
        public void AddMsgSent(string channel, string message)
        {
            object thisObj = GetObject(channel, message);
            DateTime value;
            if (msgTimes.TryGetValue(thisObj, out value))
            {
                TimeSpan msgDelay = DateTime.Now - value;
                AddMsgDelay(msgDelay);
                msgTimes.TryRemove(thisObj, out _);
            }

            checkOldMsg();
        }

        public void AddRFCommandCount()
        {
            this._userRFCommandCount++;
            this._userTotalRFCommandCount++;
        }

        //Methods
        public void AddTwitchAttempt()
        {
            this._twitchConnectionCurrentAttempt++;
            this._twitchConnectionTotalAttempt++;
        }

        public void AddTwitchDisconnect()
        {
            this._twitchConnectionTotalDisconnect++;
        }

        public void AddTwitchError()
        {
            this._twitchConnectionTotalErrorCount++;
            this._twitchConnectionCurrentErrorCount++;
        }

        public void AddTwitchSuccess()
        {
            this._twitchConnectionTotalSuccess++;
        }

        public TimeSpan avgMsgDelays()
        {
            return TimeSpanExtensions.Average(listMsgDelay.AsEnumerable());
        }

        public void JoinedChannel(string channel)
        {
            //throw new NotImplementedException();
        }

        public void LeftChannel(string channel)
        {
            //throw new NotImplementedException();
        }

        public void ReceivedLog()
        {
            //throw new NotImplementedException();
        }

        public void ResetReceivedCount()
        {
            this._userMsgRFCmdCount = 0;
            this._userRFCommandCount = 0;
        }

        public void ResetTwitchAttempt()
        {
            this._twitchConnectionCurrentAttempt = 0;
            this._twitchConnectionCurrentErrorCount = 0;
        }

        private void AddMsgDelay(TimeSpan msgDelay)
        {
            if (listMsgDelay.Count == 100)
                listMsgDelay.TryDequeue(out _);

            listMsgDelay.Enqueue(msgDelay);
        }

        private async void checkOldMsg()
        {

            foreach (var items in msgTimes)
            {
                //items.Value
            }
        }

        private object GetObject(string channel, string message)
        {
            channel = channel.ToLower().Trim();
            message = message.ToLower().Trim();

            return channel + message;
        }
    }
}