using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Twitch;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class DungeonCommandProcessor : CommandProcessor
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
        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Send(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
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
                    broadcaster.Send(cmd.Sender.Username, Localization.PERMISSION_DENIED);
                    return;
                }

                //if (cmd.Arguments.Contains("start", StringComparison.OrdinalIgnoreCase))
                //{
                //    var isSubscriber = cmd.Sender.IsSubscriber && !cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator;
                //    if (isSubscriber)
                //    {
                //        var user = userStore.Get(cmd.Sender.Username);
                //        var command = nameof(RaidCommandProcessor);
                //        if (!user.CanUseCommand(command))
                //        {
                //            var timeLeft = user.GetCooldown(command);
                //            broadcaster.Broadcast($"{cmd.Sender.Username}, You must wait another {Math.Floor(timeLeft.TotalSeconds)} secs to use that command.");
                //            return;
                //        }
                //        user.UseCommand(command, TimeSpan.FromHours(1));
                //    }

                //    await this.game.RaidStartAsync(player);
                //    return;
                //}             
            }
        }
    }
}