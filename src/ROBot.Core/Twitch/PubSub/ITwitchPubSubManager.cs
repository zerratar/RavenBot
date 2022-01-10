using System;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Twitch
{
    public interface ITwitchPubSubManager : IDisposable
    {
        event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;
        event EventHandler<OnListenResponseArgs> OnListenFailBadAuth;

        string GetActivationLink(string userId, string username);
        bool PubSubConnect(string channel);
        void Disconnect(string channel, bool rejected=false);
        bool IsReady(string channel);
    }
}
