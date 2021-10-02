using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch
{
    public interface ITwitchCommandClient : IDisposable
    {
        void Start();
        void Stop();
        void Broadcast(IGameSessionCommand message);
        void Broadcast(string channel, string user, string format, params object[] args);
        void SendChatMessage(string channel, string message);
        void JoinChannel(string channel);
        void LeaveChannel(string channel);
        bool InChannel(string name);
        IReadOnlyList<TwitchLib.Client.Models.JoinedChannel> JoinedChannels();
        long GetCommandCount();
        long GetMessageCount();
    }
}