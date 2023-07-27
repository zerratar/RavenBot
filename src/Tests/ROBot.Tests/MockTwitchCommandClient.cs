using RavenBot.Core.Handlers;
using ROBot.Core;
using ROBot.Core.Chat.Twitch;
using System.Threading.Tasks;
using TwitchLib.Communication.Events;

namespace ROBot.Tests
{
    public class MockTwitchCommandClient : ITwitchCommandClient
    {
        //public event System.EventHandler<OnLogArgs> OnTwitchLog;
        public event System.EventHandler<OnErrorEventArgs> OnTwitchError;

        public void Broadcast(string channel, string user, string format, params object[] args)
        {
            var a = "";
            if (args != null)
            {
                a = string.Join(" ", args);
            }
            System.Console.WriteLine(user + ": " + format + ", @" + channel + " " + a);
        }

        public void Broadcast(ICommandChannel channel, string user, string format, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        public void Broadcast(SessionGameMessageResponse message)
        {
            throw new System.NotImplementedException();
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

        public Task SendChatMessageAsync(string channel, string message)
        {
            throw new System.NotImplementedException();
        }

        public Task SendChatMessageAsync(ICommandChannel channel, string message)
        {
            throw new System.NotImplementedException();
        }

        public void SendMessage(ICommandChannel channel, string format, object[] args)
        {
            throw new System.NotImplementedException();
        }

        public void SendReply(ICommand command, string format, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}