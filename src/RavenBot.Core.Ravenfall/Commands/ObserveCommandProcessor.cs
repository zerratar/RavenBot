using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Twitch;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ObserveCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        private readonly ITwitchUserStore userStore;

        public ObserveCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider, ITwitchUserStore userStore)
        {
            this.game = game;
            this.playerProvider = playerProvider;
            this.userStore = userStore;
        }

        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {

                broadcaster.Send(cmd.Sender.Username,
                //broadcaster.Broadcast(
                    Localization.GAME_NOT_STARTED);
                return;
            }

            if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsSubscriber)
            {
                //broadcaster.Broadcast(
                broadcaster.Send(cmd.Sender.Username,
                    "You do not have permission to set the currently observed player.");
                return;
            }
            var isSubscriber = cmd.Sender.IsSubscriber && !cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator;
            if (isSubscriber)
            {
                var user = userStore.Get(cmd.Sender.Username);
                var command = nameof(ObserveCommandProcessor);
                if (!user.CanUseCommand(command))
                {
                    var timeLeft = user.GetCooldown(command);
                    broadcaster.Broadcast($"{cmd.Sender.Username}, You must wait another {Math.Floor(timeLeft.TotalSeconds)} secs to use that command.");
                    return;
                }

                user.UseCommand(command, TimeSpan.FromSeconds(120));
            }

            var targetPlayerName = cmd.Arguments?.Trim();
            Models.Player player = null;
            if (!string.IsNullOrEmpty(targetPlayerName))
            {
                player = playerProvider.Get(targetPlayerName);
            }
            else
            {
                player = playerProvider.Get(cmd.Sender);
            }

            await game.ObservePlayerAsync(player);

        }
    }
}
