using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Twitch;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class DungeonCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        private readonly ITwitchUserStore userStore;
        public DungeonCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider, ITwitchUserStore userStore)
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
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                await this.game.JoinDungeonAsync(player);
                return;
            }

            if (!string.IsNullOrEmpty(cmd.Arguments))
            {
                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsSubscriber)
                {
                    broadcaster.Broadcast(cmd.Sender.Username, Localization.PERMISSION_DENIED);
                    return;
                }

                await this.game.DungeonStartAsync(player);
            }
        }
    }
}