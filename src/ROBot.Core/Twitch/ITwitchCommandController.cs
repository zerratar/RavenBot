using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Twitch
{
    public interface ITwitchCommandController
    {
        Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ChatCommand cmd);
        Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ChatMessage msg);
        Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, OnChannelPointsRewardRedeemedArgs reward);
    }
}