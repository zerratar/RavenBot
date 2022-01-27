using ROBot.Core.GameServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Events;

namespace ROBot.Core.Twitch
{
    public interface ITwitchCommandController
    {
        Task<bool> HandleAsync(IBotServer game, ITwitchCommandClient twitch, ChatCommand cmd);
        Task<bool> HandleAsync(IBotServer game, ITwitchCommandClient twitch, ChatMessage msg);
        Task<bool> HandleAsync(IBotServer game, ITwitchCommandClient twitch, OnChannelPointsRewardRedeemedArgs reward);
        ICollection<Type> RegisteredCommandHandlers { get; }
        ITwitchCommandHandler GetHandler(string cmd);
    }
}