using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Models;
using RavenBot.Core.Twitch;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class RaidCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        private readonly ITwitchUserStore userStore;
        public RaidCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider, ITwitchUserStore userStore)
        {
            this.game = game;
            this.playerProvider = playerProvider;
            this.userStore = userStore;
        }
        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);
            var isRaidWar = cmd.Command.Contains("war", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                if (isRaidWar)
                {
                    return;
                }

                await this.game.JoinRaidAsync(new EventJoinRequest(player, null));
                return;
            }

            if (!string.IsNullOrEmpty(cmd.Arguments))
            {
                if (cmd.Arguments.Contains("start", StringComparison.OrdinalIgnoreCase))
                {
                    await this.game.RaidStartAsync(player);
                    return;
                }

                if (cmd.Arguments.Contains("stop", StringComparison.OrdinalIgnoreCase))
                {
                    await this.game.StopRaidAsync(player);
                    return;
                }

                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsGameAdmin)
                {
                    broadcaster.Broadcast(cmd.Sender.Username, Localization.PERMISSION_DENIED);
                    return;
                }

                var target = playerProvider.Get(cmd.Arguments);
                await this.game.RaidStreamerAsync(target, isRaidWar);
            }
        }
    }
}