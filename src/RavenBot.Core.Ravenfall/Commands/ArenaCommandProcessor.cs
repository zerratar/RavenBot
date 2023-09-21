using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ArenaCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public ArenaCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                chat.AnnounceAsync(Localization.GAME_NOT_STARTED);
                return;
            }

            // --- PLayers only ---
            //!arena leave
            //!arena, !arena join

            // --- Broadcaster Only ---
            //!arena start,begin
            //!arena cancel,cancel
            //!arena kick <player>
            //!arena add <player>

            var command = cmd.Arguments?.Trim().ToLower();
            var player = playerProvider.Get(cmd);
            if (string.IsNullOrEmpty(command) || command.Equals("join"))
            {
                await this.game[cmd.CorrelationId].JoinArenaAsync(player);
            }
            else if (command.Equals("leave"))
            {
                await this.game[cmd.CorrelationId].LeaveArenaAsync(player);
            }
            else if (command.Equals("start") || command.Equals("begin"))
            {
                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                {
                    await chat.SendReplyAsync(cmd, Localization.ARENA_PERM_FORCE);
                    return;
                }

                await this.game[cmd.CorrelationId].StartArenaAsync(player);
            }
            else if (command.Equals("cancel") || command.Equals("end"))
            {
                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                {
                    await chat.SendReplyAsync(cmd, Localization.ARENA_PERM_CANCEL);
                    return;
                }

                await this.game[cmd.CorrelationId].CancelArenaAsync(player);
            }
            else
            {
                if (command.StartsWith("kick "))
                {
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                    {
                        await chat.SendReplyAsync(cmd, Localization.ARENA_PERM_KICK);
                        return;
                    }
                    var targetPlayerName = command.Split(' ').LastOrDefault();
                    var targetPlayer = playerProvider.Get(targetPlayerName);
                    await this.game[cmd.CorrelationId].KickPlayerFromArenaAsync(player, targetPlayer);
                }
                else if (command.StartsWith("add "))
                {
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                    {
                        //broadcaster.Broadcast(
                        await chat.SendReplyAsync(cmd, Localization.ARENA_PERM_ADD);
                        return;
                    }

                    var targetPlayer = playerProvider.Get(cmd.Arguments);
                    await this.game[cmd.CorrelationId].AddPlayerToArenaAsync(player, targetPlayer);
                }
            }
        }
    }
}