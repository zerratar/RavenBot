using System;
using System.Threading.Tasks;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ObserveCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        private readonly ITwitchUserStore userStore;

        public ObserveCommandProcessor(IRavenfallClient game, IUserProvider playerProvider, ITwitchUserStore userStore)
        {
            this.RequiresBroadcaster = true;
            this.game = game;
            this.playerProvider = playerProvider;
            this.userStore = userStore;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsSubscriber && !cmd.Sender.IsVip)
            {
                await chat.SendReplyAsync(cmd, Localization.OBSERVE_PERM);
                return;
            }
            var isSubscriber = cmd.Sender.IsSubscriber && !cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsVip;
            if (isSubscriber)
            {
                var user = userStore.Get(cmd.Sender.Username);
                var command = nameof(ObserveCommandProcessor);
                if (!user.CanUseCommand(command))
                {
                    var timeLeft = user.GetCooldown(command);
                    await chat.SendReplyAsync(cmd, Localization.COMMAND_COOLDOWN, Math.Floor(timeLeft.TotalSeconds));
                    return;
                }

                user.UseCommand(command, TimeSpan.FromSeconds(120));
            }

            var targetPlayerName = cmd.Arguments?.Trim();
            Models.User player = null;
            if (!string.IsNullOrEmpty(targetPlayerName))
            {
                player = playerProvider.Get(targetPlayerName);
            }
            else
            {
                player = playerProvider.Get(cmd);
            }

            await this.game[cmd.CorrelationId].ObservePlayerAsync(player);

        }
    }
}
