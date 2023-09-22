using System;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Chat.Twitch.PubSub
{
    public interface ITwitchPubSubManager : IDisposable
    {
        event EventHandler<OnChannelPointsRewardRedeemedArgs> OnChannelPointsRewardRedeemed;
        TwitchPubSubClient GetPubSubClient(string channel);
        string GetActivationLink();
        void PubSubConnect(string channel);
        void Disconnect(string channel, bool logRemoval = true);
        bool IsReady(string channel);
    }
}
