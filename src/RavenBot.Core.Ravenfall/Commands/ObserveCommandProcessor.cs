using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Handlers;

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

            var targetPlayerName = cmd.Arguments?.Trim();
            Models.User player = null;
            if (!string.IsNullOrEmpty(targetPlayerName))
            {
                if ((!IsIsland(targetPlayerName) || !cmd.IsVipPlus()) && !cmd.IsModeratorPlus())
                {
                    await chat.SendReplyAsync(cmd, Localization.OBSERVE_PERM);
                    return;
                }
                else
                {
                    player = playerProvider.Get(targetPlayerName);
                }
            }
            else
            {
                player = playerProvider.Get(cmd);
            }

            await this.game[cmd.CorrelationId].ObservePlayerAsync(player);

        }
        private static bool IsIsland(string targetPlayerName)
        {
            return Equals(targetPlayerName, "home")
                         || Equals(targetPlayerName, "away")
                         || Equals(targetPlayerName, "ironhill")
                         || Equals(targetPlayerName, "heim")
                         || Equals(targetPlayerName, "kyo")
                         || Equals(targetPlayerName, "atria")
                         || Equals(targetPlayerName, "eldara");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(string str, string other)
        {
            return string.Equals(str, other, StringComparison.OrdinalIgnoreCase);
        }
    }
}
