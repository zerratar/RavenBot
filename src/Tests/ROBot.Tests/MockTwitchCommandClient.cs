using ROBot.Core.Twitch;
using System.Collections.Generic;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace ROBot.Tests
{
    public class MockTwitchCommandClient : ITwitchCommandClient
    {
        public event System.EventHandler<OnLogArgs> OnTwitchLog;
        public event System.EventHandler<OnErrorEventArgs> OnTwitchError;

        public void Broadcast(IGameSessionCommand message)
        {
        }

        public void Broadcast(string channel, string user, string format, params object[] args)
        {
            var a = "";
            if (args != null)
            {
                a = string.Join(" ", args);
            }
            System.Console.WriteLine(user + ": " + format + ", @" + channel + " " + a);
        }

        public void Dispose()
        {
        }

        public bool InChannel(string name)
        {
            return true;
        }

        public void JoinChannel(string channel)
        {
        }
        public void LeaveChannel(string channel)
        {
        }

        public void SendChatMessage(string channel, string message)
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}