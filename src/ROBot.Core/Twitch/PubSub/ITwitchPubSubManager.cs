using System;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Twitch
{
    public interface ITwitchPubSubManager : IDisposable
    {
        event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;
        event EventHandler<OnListenResponseArgs> OnListenFailBadAuth;

        string GetActivationLink(string userId, string username);
        bool Connect(string channel);
        void Disconnect(string channel);
        bool IsReady(string channel);
    }
}
