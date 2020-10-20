using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ArenaCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ArenaCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(Localization.GAME_NOT_STARTED);
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
            var player = playerProvider.Get(cmd.Sender);
            if (string.IsNullOrEmpty(command) || command.Equals("join"))
            {
                await game.JoinArenaAsync(player);
            }
            else if (command.Equals("leave"))
            {
                await game.LeaveArenaAsync(player);
            }
            else if (command.Equals("start") || command.Equals("begin"))
            {
                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                {
                    broadcaster.Broadcast(cmd.Sender.Username, Localization.ARENA_PERM_FORCE);
                    return;
                }

                await game.StartArenaAsync(player);
            }
            else if (command.Equals("cancel") || command.Equals("end"))
            {
                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                {
                    broadcaster.Broadcast(cmd.Sender.Username, Localization.ARENA_PERM_CANCEL);
                    return;
                }

                await game.CancelArenaAsync(player);
            }
            else
            {
                if (command.StartsWith("kick "))
                {
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                    {
                        broadcaster.Broadcast(cmd.Sender.Username, Localization.ARENA_PERM_KICK);
                        return;
                    }
                    var targetPlayerName = command.Split(' ').LastOrDefault();
                    var targetPlayer = playerProvider.Get(targetPlayerName);
                    await game.KickPlayerFromArenaAsync(player, targetPlayer);
                }
                else if (command.StartsWith("add "))
                {
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                    {
                        //broadcaster.Broadcast(
                        broadcaster.Broadcast(cmd.Sender.Username, Localization.ARENA_PERM_ADD);
                        return;
                    }

                    var targetPlayer = playerProvider.Get(cmd.Sender);
                    await game.AddPlayerToArenaAsync(player, targetPlayer);
                }
            }
        }
    }
}