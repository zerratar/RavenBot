using ROBot.Core.Twitch;
using System.Collections.Generic;
using TwitchLib.Client.Models;

namespace ROBot.Tests
{
    public class MockTwitchCommandClient : ITwitchCommandClient
    {
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

        public long GetCommandCount()
        {
            return 0;
        }

        public long GetMessageCount()
        {
            return 0;
        }

        public bool InChannel(string name)
        {
            return true;
        }

        public void JoinChannel(string channel)
        {
        }

        public IReadOnlyList<JoinedChannel> JoinedChannels()
        {
            return new List<JoinedChannel>();
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